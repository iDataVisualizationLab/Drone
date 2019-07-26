using System;
using System.Collections.Generic;
using Windows.Data.Json;

namespace Brain_uwp.Data
{
    /// <summary>
    /// Data strut for keeping data about OpenALPR result
    /// </summary>
	class OpenAlprData
	{
        /// <summary>
        /// Possible plates that is detected by the OpenALPR
        /// </summary>
		public Dictionary<string, float> possible_plate { get; set; }
        /// <summary>
        /// Message that is returned from the server
        /// </summary>
		public string message { get; set; }
        /// <summary>
        /// File name of the image that is processed
        /// </summary>
		public string filename { get; set; }

		public void Parse(JsonObject json)
		{
			possible_plate = new Dictionary<string, float>();
			filename = json.GetNamedString("filename");
			message = json.GetNamedString("message").ToLower();
			IJsonValue value;
			if (json.TryGetValue("possible_plate",out value))
			{
				JsonObject jsonValues = value.GetObject(); 
				foreach(var val in jsonValues)
				{
					possible_plate.Add(val.Key.ToLower() ,  float.Parse(val.Value.ToString()));
				}
			}
			else
			{
				System.Diagnostics.Debug.WriteLine("THIS HERE WORKED");
			}
		}

		public void Parse(string msg)
		{
			string[] tokens = msg.ToLower().Split(" ");
	
			possible_plate = new Dictionary<string, float>();
			for (var i = 0; i < tokens.Length - 3; i++)
			{
				if (tokens[i].Equals("-"))
				{
					System.Diagnostics.Debug.WriteLine("|"+ tokens[i + 1].Trim() + "|"+ tokens[i + 3].Replace('\n', ' ') + "|");
					float value;
					if(float.TryParse(tokens[i + 3].Replace('\n', ' ').Trim(),out value))
					{
						possible_plate.Add(tokens[i+1].Trim() , value);
					}
				}
			}
		}

        /// <summary>
        /// Gets the most confident plate.
        /// </summary>
        /// <returns>Most confident palte</returns>
		public Pair<string , float> GetMostConfidentPlate()
		{
			Pair<string, float> result = new Pair<string, float>("", float.MinValue);

			foreach(var tuple in possible_plate)
			{
				if (tuple.Value > result.Second)
				{
					result.First = tuple.Key;
					result.Second = tuple.Value;
				}
			}

			return result;
		}

		public override string ToString()
		{
			string result = "{\n filename:" + filename + ",\n message:" + message + ",\n possible_plate[\n";
			foreach(var pp in possible_plate)
			{
				result += "	 " + pp.Key + ":" + pp.Value + ",\n";
			}
			result += " ]\n}";
			return result;
		}
	}
}
