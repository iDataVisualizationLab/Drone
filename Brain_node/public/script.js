var socket = io(); // socket that will connect to the server

var plate_picture = new Object(); // An array that stores refrences to plates and coresponding pictures

var map = L.map('mapid').setView([33.586790, -101.876584], 30); // initilize map near the TTU

var drone_lat = 0; // drone latidue this value will be updated by server
var drone_lon = 0; // drone longitude this value will be updated by server

var drone_marker = L.marker([drone_lat , drone_lon]); // drone marker on the map
drone_marker.addTo(map);

//var markers = new Array();
var markers = L.layerGroup().addTo(map); // markers of detected cars

//init the map with layer 
L.tileLayer('https://api.tiles.mapbox.com/v4/{id}/{z}/{x}/{y}.png?access_token=_tokenHERE_', {
    attribution: 'Map data &copy; <a href="https://www.openstreetmap.org/">OpenStreetMap</a> contributors, <a href="https://creativecommons.org/licenses/by-sa/2.0/">CC-BY-SA</a>, Imagery © <a href="https://www.mapbox.com/">Mapbox</a>',
    maxZoom: 18,
    id: 'mapbox.streets'
}).addTo(map);


//Code bellow actually splits the found and not found plates but currently it is not possible detect found or not found plates
//so this is here just for a refrence
/*
socket.on('data_event', (plate) => {
    let detected_plate; 
    if(plate.plate != ""){
        detected_plate = L.marker([plate.lat, plate.long],{icon:blue_marker});
    }else{
        detected_plate = L.marker([plate.lat, plate.long],{icon:red_marker});
    }

    detected_plate.addTo(map);
   
    let div = document.createElement('div');
    
    let img = document.createElement('img');
    img.style.width = "800px";
    img.style.height = "600px";
    img.src = 'data:image/jpeg;base64, ' + base64ArrayBuffer(plate.buffer);
   
    let input_plate_tex = document.createElement("input");
    input_plate_tex.setAttribute('type', 'text');
    input_plate_tex.id = plate['_id'];
    input_plate_tex.value = plate['plate'];

    let input_button = document.createElement("button");
    input_button.innerHTML = "Update Plate";
    input_button.onclick = function () {
        plate.plate = input_plate_tex.value;
        let updated_data = {
            type: "uptade_plate",
            data: plate
        };
        socket.emit("update_event", updated_data);
    };

    div.appendChild(img);
    div.appendChild(input_plate_tex);
    div.appendChild(input_button);
    detected_plate.bindPopup(div);

});
*/

//Request all the plates as soon as you connected
socket.emit("request_event", {
    type: "get_all_plates"
});

//Recevie drone_position events from the server
socket.on("drone_position" , (drone_pos) => {
    drone_marker.setLatLng([drone_pos.lat , drone_pos.long]);    
})

//User press this button after selecting filtering options
document.getElementById("confirm_butt").onclick = function () {
    clearMapMarkers();
    plate_picture = new Object(); /*remove old stuff if it has any*/
    let elem = document.getElementById("searchOption");
    let selected = elem.options[elem.selectedIndex].value;
    
    let ts = document.getElementById("time_start").value;
    let te = document.getElementById("time_end").value;
    if (selected == "unknown_plates") {

        let request = {
            type: "unknown_plates",
            data: "",
            time_start: ts,
            time_end: te
        };
        socket.emit("request_event", request);
    } else if (selected == "live_plates") { 
        /* setInterval already requests the plates each 1 second*/
    } else if (!document.getElementById("searchPlate").disabled && selected == "search_plates") {
        let text = document.getElementById("searchPlate").value;
        let request = {
            type: "search_plates",
            data: text,
            time_start: ts,
            time_end: te
        };
        socket.emit("request_event", request);
    }
};

//Disable search bar according the search options, so visualy its more helpfull
document.getElementById("searchOption").onchange = function () {
    let searchBar = document.getElementById("searchPlate");
    let elem = document.getElementById("searchOption");
    let selected = elem.options[elem.selectedIndex].value;
    if (selected == "unknown_plates") {
        searchBar.disabled = true;
    } else if (selected == "live_plates") {
        searchBar.disabled = true;
    } else if (selected == "search_plates") {
        searchBar.disabled = false;
    }
};

setInterval(() => {
    //If current search options is live_plates requsest the latest plates at 1 second interval
    let elem = document.getElementById("searchOption");
    let selected = elem.options[elem.selectedIndex].value;
    if(selected == "live_plates"){
        let request = {
            type: "live_plates",
            data: ""
        };
        socket.emit('request_event', request);
    }
    
}, 1000);

//If data is begin transmitted, disable all the important buttons so programs does not crash
var time_start_disabled_state;
var time_end_disabled_state;
var confirm_butt_disabled_state;
var searchOption_disabled_state;
var searchPlate_disabled_state;
socket.on("data_begin",function(){

    time_start_disabled_state = document.getElementById("time_start").disabled;
    time_end_disabled_state = document.getElementById("time_end").disabled ;
    confirm_butt_disabled_state = document.getElementById("confirm_butt").disabled;
    searchOption_disabled_state = document.getElementById("searchOption").disabled ;
    searchPlate_disabled_state = document.getElementById("searchPlate").disabled;

    document.getElementById("loading_text").innerHTML = "Loading Data Please Be Patient"; /* Cuz program is sloww */
    document.getElementById("time_start").disabled = true;
    document.getElementById("time_end").disabled = true;
    document.getElementById("confirm_butt").disabled = true;
    document.getElementById("searchOption").disabled = true;
    document.getElementById("searchPlate").disabled = true;
});

//Restore back all the importnat buttons after data_end finished
socket.on("data_end", function(){
    document.getElementById("loading_text").innerHTML = "Done!";
    document.getElementById("time_start").disabled = time_start_disabled_state;
    document.getElementById("time_end").disabled = time_end_disabled_state;
    document.getElementById("confirm_butt").disabled = confirm_butt_disabled_state;
    document.getElementById("searchOption").disabled = searchOption_disabled_state;
    document.getElementById("searchPlate").disabled = searchPlate_disabled_state;
});

//Live video feed parsing and stuff happens here
const liveFeed = document.getElementById("liveFeed"); // this is the canvas
liveFeed.width = 400;
liveFeed.height = 300; 
socket.on('liveFeedClient', (data)=>{ /* when client receives video data */
   liveFeed.src = 'data:image/png;base64, ' + base64ArrayBuffer(data);
});

//when client receives data about plates
socket.on('data_event', (plate) => {
    if (plate.hasOwnProperty('plate')) {
        let detected_plate_marker = L.marker([plate.lat, plate.long]);
     /*   if(plate.plate != ""){
            detected_plate_marker = L.marker([plate.lat, plate.long],{icon:blue_marker});
        }else{
            detected_plate_marker = L.marker([plate.lat, plate.long],{icon:red_marker});
        }*/
    
        //var img = document.createElement('img');
        //img.src = 'data:image/jpeg;base64, ' + base64ArrayBuffer(plate.buffer);
        
        let li = document.createElement("li");
        let div = document.createElement("div");
        div.classList.add("container");
        div.id = plate['_id'];

        let input_plate_tex = document.createElement("input");
        input_plate_tex.setAttribute('type', 'text');
        input_plate_tex.id = plate['_id'];

        let input_button = document.createElement("button");
        input_button.innerHTML = "Update Plate";
        input_button.onclick = function () {
            console.log(" " + input_plate_tex.id + " is clicked");
            plate.plate = input_plate_tex.value;
            let updated_data = {
                type: "uptade_plate",
                data: plate
            };
            socket.emit("update_event", updated_data);
        };

        let show_image_button = document.createElement("button");
        show_image_button.innerHTML = "Show Image";

        plate_picture[plate['_id']] = { buf: plate.buffer, isOpen: "false" };

        show_image_button.onclick = function () {
            let d = document.getElementById(plate['_id']);
            if (plate_picture[plate['_id']]['isOpen'] === "false") {
                let img = document.createElement('img');
                img.style.width = "800px";
                img.style.height = "600px";
                plate_picture[plate['_id']]['isOpen'] = "true";
                img.src = 'data:image/jpeg;base64, ' + base64ArrayBuffer(plate_picture[plate['_id']]['buf']);
                document.getElementById(plate['_id']).appendChild(img);
            } else {
                plate_picture[plate['_id']]['isOpen'] = "false";
                console.log("A" + d.childNodes.length);
                console.log("B" + d.childElementCount);
                d.removeChild(d.childNodes[d.childNodes.length - 1]);/*remove last child*/
            }

            document.body.style.zoom = 1.0000001;
            setTimeout(function () { document.body.style.zoom = 1; }, 50);
        }

        input_plate_tex.value = plate.plate;

        li.appendChild(document.createTextNode("Plate Name: "));
        li.appendChild(input_plate_tex);
        li.appendChild(document.createElement("br"));
        li.appendChild(document.createTextNode("Detected time: " + plate.timeStamp));
        li.appendChild(document.createElement("br"));
        li.appendChild(document.createTextNode("Latitude: " + plate.lat));
        li.appendChild(document.createElement("br"));
        li.appendChild(document.createTextNode("Longitude " + plate.long));
        li.appendChild(document.createElement("br"));
        li.appendChild(input_button);
        li.appendChild(show_image_button);
        div.appendChild(li);

        // div.appendChild(img);
        detected_plate_marker.bindPopup(div);
        detected_plate_marker.addTo(markers);
        
        //markers.addLayer(detected_plate_marker);
       // markers.push(detected_plate_marker);
    }
});

function arrayToString(bufferValue) {
    return new TextDecoder("utf-8").decode(bufferValue);
}

function base64ArrayBuffer(arrayBuffer) {
    var base64 = ''
    var encodings = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/'

    var bytes = new Uint8Array(arrayBuffer)
    var byteLength = bytes.byteLength
    var byteRemainder = byteLength % 3
    var mainLength = byteLength - byteRemainder

    var a, b, c, d
    var chunk

    // Main loop deals with bytes in chunks of 3
    for (var i = 0; i < mainLength; i = i + 3) {
        // Combine the three bytes into a single integer
        chunk = (bytes[i] << 16) | (bytes[i + 1] << 8) | bytes[i + 2]

        // Use bitmasks to extract 6-bit segments from the triplet
        a = (chunk & 16515072) >> 18 // 16515072 = (2^6 - 1) << 18
        b = (chunk & 258048) >> 12 // 258048   = (2^6 - 1) << 12
        c = (chunk & 4032) >> 6 // 4032     = (2^6 - 1) << 6
        d = chunk & 63               // 63       = 2^6 - 1

        // Convert the raw binary segments to the appropriate ASCII encoding
        base64 += encodings[a] + encodings[b] + encodings[c] + encodings[d]
    }

    // Deal with the remaining bytes and padding
    if (byteRemainder == 1) {
        chunk = bytes[mainLength]

        a = (chunk & 252) >> 2 // 252 = (2^6 - 1) << 2

        // Set the 4 least significant bits to zero
        b = (chunk & 3) << 4 // 3   = 2^2 - 1

        base64 += encodings[a] + encodings[b] + '=='
    } else if (byteRemainder == 2) {
        chunk = (bytes[mainLength] << 8) | bytes[mainLength + 1]

        a = (chunk & 64512) >> 10 // 64512 = (2^6 - 1) << 10
        b = (chunk & 1008) >> 4 // 1008  = (2^6 - 1) << 4

        // Set the 2 least significant bits to zero
        c = (chunk & 15) << 2 // 15    = 2^4 - 1

        base64 += encodings[a] + encodings[b] + encodings[c] + '='
    }

    return base64
}

function clearMapMarkers(){
    markers.clearLayers();
}
