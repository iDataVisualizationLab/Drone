const { exec } = require('child_process'); // allows to run external programs

var express = require('express'); // Main module for server connection
var http = require('http'); // used for socket connection
var fs = require("fs"); // file io
var bodyParser = require('body-parser'); // parsers the post commands
var multer = require('multer'); // used in post request

var mongoose = require("mongoose"); // mongo db
mongoose.connect("mongodb://localhost/drone_interface", { useNewUrlParser: true }); // connect to the mongo 

var detectionSchema = new mongoose.Schema({ /* mongo scheme used for connecting mongo db*/
    plate: String,
    imgName: String,
    timeStamp: Date,
    lat: Number,
    long: Number
});

var DetectedPlates = mongoose.model("DetectedPlates", detectionSchema); /* use the schema to connect to the db */

//make sure you keep this order
//this is the initilation of the server
var app = express();
var server = http.createServer(app);
var io = require('socket.io').listen(server);

app.use(express.static('public')); // use public folder for all file refrences in the client side
app.use(bodyParser.urlencoded({ extended: false })); 

var upload = multer({ dest: '/tmp' });
var findRemoveSync = require('find-remove'); 

var users = [];

var drone_lat;
var drone_lon;

setInterval(() => { // this removes everything in the /data folder which is older than 120 secs
    var result = findRemoveSync(__dirname + '/data', { age: { seconds: 120 }, extensions: ['.jpg', '.png'] });
    //console.log(result);
}, 120000);


//this runs when clients connect
io.on('connection', (socket) => {
    console.log(socket + " is connected");
    //create a ref user
    let user = {
        sock: socket,
        requested_data_type: "live_plates",
        last_time_emitted: Date.now()
    };

    //put user to the array so we can accsess later
    users.push(user);

    /* socket.on('data', (data) => {
         if (data == null) {
             console.log(err);
             return;
         }
         console.log("here " + data['width'] + ',' + data['height'] + "," + data["data"].length);
         for (let i = 0; i < users.length; i++) {
             users[i]['sock'].emit("liveFeedClient", data);
         }
     });*/

    //TODO:  drone_loc shoult not be in here
    //NOTE: drone_loc is emitted by Main application which does not make sense for that to be in here
    //because this connections are ment to be for the client side, so this means any client could
    //potentially emit this data, but this data just for visual nothing important is done based on this data
    socket.on('drone_loc' /* This is emitten only by Main application */, (drone_loc) => { 
        drone_lat = drone_loc['lat'];
        drone_lon = drone_loc['lon'];
        // console.log("Drone Loc: " + drone_lat + ", "+ drone_lon);
        //After receiving the data, broadcast to the all clients
        for (let i = 0; i < users.length; i++) {
            users[i]['sock'].emit("drone_position", { lat: drone_lat, long: drone_lon })
        }
    });

    //Client wants to update a plate.
    socket.on("update_event", (updated_data) => {
        //Check for mongodb docs about update()
        DetectedPlates.update({ "_id": updated_data['data']['_id'] }, { $set: { "plate": updated_data['data']['plate'] } }, function (err, result) {
            if (err) {
                console.log(err);
            }
        });
    });
    //Client request a data
    socket.on('request_event', (data) => {
        let requested_data_type = data['type'];
        user['requested_data_type'] = requested_data_type;

        if (user['requested_data_type'] == "live_plates") { /* if data type is live */
            DetectedPlates.find({ "timeStamp": { "$gte": user['last_time_emitted'] } }, (err, plates) => {
                if (err) {
                    console.log(err);
                } else {
                    if (plates.length > 0) {
                        user['sock'].emit("data_begin");
                        for (let i = 0; i < plates.length; i++) {
                            var buf = fs.readFileSync(__dirname + "/detected_plates/" + plates[i].imgName);
                            let plate = jsonCopy(plates[i]);
                            plate['buffer'] = buf;
                            console.log("lenght of " + plate.imgName + " is " + plate['buffer'].length);
                            user['last_time_emitted'] = Date.now();
                            user['sock'].emit('data_event', plate);
                        }
                        user['sock'].emit("data_end");
                    }
                }
            });
        } else if (user['requested_data_type'] == "unknown_plates") { 
            //TODO: THIS CURRENTLY DOES NOT WORKS BECAUSE EVERY PICTURE THAT DOES NOT HAVE RECOQNIZED PLATE IS DISCARDED
            //WHEN DATABASE OF REAL PLATES IS AVALIBE THIS HERE WILL SEND THOSE PALTES THAT ARE RECOQNIZED, BUT IS CONTAINT IN THE DATABASE
            DetectedPlates.find({ $and: [{ "plate": "", "timeStamp": { "$gte": Date.parse(data['time_start']), "$lt": Date.parse(data['time_end']) } }] }, (err, plates) => {
                if (err) {
                    console.log(err);
                } else {
                    if (plates.length > 0) {
                        user['sock'].emit("data_begin");
                        for (let i = 0; i < plates.length; i++) {
                            var buf = fs.readFileSync(__dirname + "/detected_plates/" + plates[i].imgName);
                            let plate = jsonCopy(plates[i]);
                            plate['buffer'] = buf;
                            //console.log("lenght of " + plate.imgName + " is " +  plate['buffer'].length);
                            user['last_time_emitted'] = Date.now();
                            user['sock'].emit('data_event', plate);
                        }
                        user['sock'].emit("data_end");
                    }
                }
            });
        } else if (user['requested_data_type'] == "search_plates") {
            //MANUALY SEARCH FOR PALTE IN A GIVEN TIME RANGE
            console.log("doing search plate " + '/' + data['data'] + '/i');
            var keyword = data['data'];
            var regex = RegExp(".*" + keyword + ".*");
            //TODO: MAKE THIS AND
            DetectedPlates.find({ "plate": regex, "timeStamp": { "$gte": Date.parse(data['time_start']), "$lt": Date.parse(data['time_end']) } }, (err, plates) => {
                if (err) {
                    console.log(err);
                } else {
                    console.log("  " + plates.length + " plates found");
                    if (plates.length > 0) {
                        user['sock'].emit("data_begin");
                        for (let i = 0; i < plates.length; i++) {
                            var buf = fs.readFileSync(__dirname + "/detected_plates/" + plates[i].imgName);
                            let plate = jsonCopy(plates[i]);
                            plate['buffer'] = buf;
                            //console.log("lenght of " + plate.imgName + " is " +  plate['buffer'].length);
                            user['last_time_emitted'] = Date.now();
                            user['sock'].emit('data_event', plate);
                        }
                        user['sock'].emit("data_end");
                    }
                }
            });
        } else if (user['requested_data_type'] == "get_all_plates") {
            //GETS ALL THE PLATES THAT ARE DETECTED, THIS IS CALLED WHEN CLIENT FIRST CONNECTS TO THE SERVER
            DetectedPlates.find({}, (err, plates) => {
                if (err) {
                    console.log(err);
                } else {
                    console.log("  " + plates.length + " plates found");
                    if (plates.length > 0) {
                        user['sock'].emit("data_begin");
                        for (let i = 0; i < plates.length; i++) {
                            var buf = fs.readFileSync(__dirname + "/detected_plates/" + plates[i].imgName);
                            let plate = jsonCopy(plates[i]);
                            plate['buffer'] = buf;
                            //console.log("lenght of " + plate.imgName + " is " +  plate['buffer'].length);
                            user['last_time_emitted'] = Date.now();
                            user['sock'].emit('data_event', plate);
                        }
                        user['sock'].emit("data_end");
                    }
                }
            });
        }


    });

    socket.on('disconnect', () => {
        console.log(socket + " is disconnected");
        for (let i = 0; i < users.length; i++) {
            if (users[i]['sock'] === socket) {
                users.splice(i, 1);
                break;
            }
        }
    });
});

//Main page of the application
app.get('/', function (req, res) {
    res.sendFile(__dirname + "/" + "index.html");
})
/* Server uploads images thourg this post call*/
app.post('/file_upload_alpr', upload.fields([{ name: 'file', maxCount: 1 }, { name: 'detected_lat', maxCount: 1 }, { name: 'detected_lon', maxCount: 1 }]), function (req, res) {
    var file = __dirname + "/data/" + req.files['file'][0].originalname;
    ///  var file_2 = __dirname + "/data/" + req.file.originalname;
    console.log("request file_upload_alpr " + req.files['file'][0].originalname);
    fs.readFile(req.files['file'][0].path, function (err, data) { /* Read the image */
        fs.writeFile(file, data, function (err) { /* Save the image into data folder */
            if (err) {
                console.error(err);
                response = {
                    message: 'Sorry, file couldn\'t be uploaded.',
                    filename: req.files['file'][0].originalname
                };
                res.end(JSON.stringify(response));
            } else {
                //console.log('alpr -c us -p tx data/' + req.file.originalname);
                /* Run the alpr command on the image that just saved */
                exec('alpr -c us -p tx data/' + req.files['file'][0].originalname, (err, stdout, stderr) => {
                    if (err) {
                        res.end("ERROR WHILE RUNNING ALRP " + err);
                    }

                    // the *entire* stdout and stderr (buffered)
                    // console.log(`stdout1: ${stdout}`);
                    // console.log(`stderr1: ${stderr}`);

                    //Parse the result of the alpr output
                    let tokens = stdout.toLowerCase().trim();
                    tokens = tokens.split(" ");
                    let possible_plates = new Object();
                    let best_possible_plate = new Object();
                    best_possible_plate['plate'] = "";
                    best_possible_plate['conf'] = -9999;

                    for (let i = 0; i < tokens.length - 3; i++) {
                        if (tokens[i] == "-") {
                            let p = tokens[i + 1].trim();
                            let c = parseFloat(tokens[i + 3]);
                            possible_plates[p] = c;
                            has_plates = true;
                            if (c > best_possible_plate['conf']) {
                                best_possible_plate['plate'] = p;
                                best_possible_plate['conf'] = c;
                            }
                        }
                    }

                    var count = 0; // count of possible plates
                    for (var k in possible_plates) {
                        if (possible_plates.hasOwnProperty(k)) {
                            ++count;
                        }
                    }

                    //console.log(possible_plates);
                    //create the response 
                    response = {
                        possible_plate: possible_plates,
                        message: 'File uploaded successfully',
                        filename: req.files['file'][0].originalname
                    };

                    console.log("Count " + count);
                    if (count > 0) { /* if count of possible plate is > 0 means this picture contains a licence plate */
                        fs.renameSync(__dirname + "/data/" + req.files['file'][0].originalname, /* so move this file to the detected_plates so it wont get removed automatically */
                        __dirname + "/detected_plates/" + req.files['file'][0].originalname)

                        console.log("Plate detected create an entry: " + best_possible_plate['plate'] + "< " + req.body['detected_lat'] + ", " + req.body['detected_lon'] + ">");
                        let plate = new DetectedPlates({ /* create new enrty on the data base */
                            plate: best_possible_plate['plate'],
                            timeStamp: Date.now(),
                            imgName: req.files['file'][0].originalname,
                            lat: req.body['detected_lat'],
                            long: req.body['detected_lon']
                        });

                        //console.log("save the plate ....");
                        plate.save((err, plt) => {
                            if (err) {
                                console.log(err);
                            }
                            // After succesfuly saving the data, respond to the caller with the result
                            //console.log("SAVED");
                            res.end(JSON.stringify(response));
                        });
                    } else {
                        //Send the result anyway if there is nothing detected
                        res.end(JSON.stringify(response));
                    }
                });
            }
        });
    });
})

//This code bellow is used to capture live video feed from camera
var net = require('net');
var bytes = []; // Total collected bytes from main application
var byteSize = 0; // Total size to be expected from server
var tcpserver = net.createServer(function (socket) {
    socket.on('data', (data) => { // when data is received
        if(byteSize == 0){ // if byteSize is not set, that means this is the beginning of the data transmission
            let header = Buffer.from(data, "ascii").toString(); // parse the bytes
          //  console.log("I got header " + header);
            if(header.includes("size=")){
                let tok = header.split("=");
                byteSize = parseFloat(tok[1]); // parse the header and get the byteSize; this is the total size of the whole data
                socket.write("ready"); // send back ready to the caller so it can start transmitting
            }else{
                socket.write("badbadbad"); 
              //  console.log("bad");
            }
        }else{
            //if byteSize is set get the data and push into bytes array
            bytes.push(...data);
            //console.log("size " + bytes.length + " byteSize " + byteSize);
            if(bytes.length >= byteSize){ // if lenght of the bytes is bigger than byteSize this means we got all the data of the video frame so send this to the all clients
             //   console.log("ended")
                for (let i = 0; i < users.length; i++) {
                    users[i]['sock'].emit("liveFeedClient", bytes); // send collected bytes which is a video frame to the clients
                }
                bytes = []; // clear bytes for next transmission
                byteSize = 0; // clear for next
            //    console.log("hmm");
            }
        }
    });
});

tcpserver.listen(1337, '127.0.0.1');

server.listen(8081, () => {
    console.log("Server started at port 8081");
});

function jsonCopy(src) {
    return JSON.parse(JSON.stringify(src));
}