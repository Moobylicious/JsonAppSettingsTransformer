using NDesk.Options.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace jsonSetter
{
    class Program
    {
        private enum ReturnValues
        {
            OK=0,
            InvalidArgs = 1,
            Missingfile = 2,
            PropertyNotFound = 3,
            OtherError = 3
        }

        private static void Log(string s)
        {
            Console.WriteLine(s);
        }

        static int Main(string[] args)
        {
            var opts = new RequiredValuesOptionSet();
            var fileName = opts.AddVariable<string>("filename", "the appSettings.json filename.  defaults to appSettings.json");
            var isDelete = opts.AddSwitch("delete", "delete the setting from the file");
            var isAdd = opts.AddSwitch("add", "add the setting if not there.");
            var settingName = opts.AddRequiredVariable<string>("setting", "the name of the setting to change/add/delete");
            var newValue = opts.AddVariable<string>("value", "the new value for the setting.");
            var cm = new ConsoleManager("Console", opts);
            if (cm.TryParseOrShowHelp(Console.Out, args))
            {
                if (isDelete && !string.IsNullOrEmpty(newValue))
                {
                    //sanity check.
                    Log("conflicting arguments - cannot specify -delete AND a new value.");
                    return (int)ReturnValues.InvalidArgs;
                }
                else if (!isDelete && string.IsNullOrEmpty(newValue))
                {
                    Log("if not deleting a setting, newvalue must be specified!");
                    return (int)ReturnValues.InvalidArgs;
                }

                string jsonFileName = fileName;
                if (string.IsNullOrEmpty(jsonFileName))
                {
                    Log("No filename specified.  assuming appSettings.json");
                    jsonFileName = "appSettings.json";
                }

                if (!File.Exists(jsonFileName))
                {
                    Log($"file {jsonFileName} not found!");
                    return (int)ReturnValues.Missingfile;
                }

                try
                {
                    Log($"Loading {jsonFileName}");
                    var fileContents = File.ReadAllText(jsonFileName);
                    Log("Deserialising file");
                    JObject settings = JsonConvert.DeserializeObject<JObject>(fileContents);

                    //try to find the wanted setting.
                    var prop = settings.SelectToken(settingName);

                    //If not found and we've been told to DELETE it,
                    //or its not found and we've not been told to add it, then 
                    //give up...
                    if (prop == null && (isDelete || !isAdd))
                    {
                        Log($"property {settingName.Value} does not exist!");
                        return (int)ReturnValues.PropertyNotFound;
                    }

                    bool okToSave = false;

                    //bool 

                    JObject parentProp = settings;
                    string thisPropertyLowLevelName = settingName;
//                    if (prop != null)
                    {
                        Log($"trying to find parent property of {settingName.Value}");
                        var proptree = settingName.Value.Split(new[] { '.' });

                        thisPropertyLowLevelName = proptree.Last();

                        parentProp = settings;

                        if (proptree.Length > 1)
                        {
                            var parentPath = string.Join(".", proptree.Take(proptree.Length - 1));

                            parentProp = (settings.SelectToken(parentPath) as JObject);

                            //If we could not find the parent prop, perhaps the ACTUAL property is an
                            //array/index property and it doesn't have an entry at that index?                            

                            if (parentProp == null && parentPath.Contains("[")){
                                var requestedItemIndexBit = parentPath.Substring(parentPath.IndexOf('['));

                                //See if the list property itself exists - strip off the index.
                                parentPath = parentPath.Substring(0, parentPath.IndexOf('['));
                                
                                var parentArrayProp = (settings.SelectToken(parentPath) as JArray);

                                //OK, if the array property exists, let's try to add objects to it until
                                //it has one at the requested index.s
                                if (parentArrayProp != null)
                                {
                                    //Get required index.
                                    if (requestedItemIndexBit.Length >= 3)
                                    {
                                        if (int.TryParse(requestedItemIndexBit.Substring(
                                            1,requestedItemIndexBit.Length-2),out var requestedIndex))
                                        {
                                            if (requestedIndex > 0)
                                            {
                                                while ((parentArrayProp.Count - 1) < requestedIndex)
                                                {
                                                    //add empty objects, we know not what their contents should be...
                                                    parentArrayProp.Add(new JObject());
                                                }
                                                parentProp = (parentArrayProp[requestedIndex] as JObject);
                                            }
                                        }
                                    }
                                }
                            }
                            Log($"trying to find property {parentPath}");
                        }                  
                    }

                    if (prop != null && isDelete)
                    {                        
                        Log($"trying to delete property {settingName.Value}");

                        //prop is a JToken, not a JProperty, so we need to get the propery
                        //by getting its parent and getting the Property directly.
                        var thisProperty = parentProp.Property(thisPropertyLowLevelName);
                        if (thisProperty == null)
                        {
                            Log("Could not find property to remove!");
                            return (int)ReturnValues.OtherError;
                        }
                        thisProperty.Remove();
                        okToSave = true;
                    }
                    else if (prop == null && isAdd)
                    {                      
                        if (parentProp == null)
                        {
                            Log("Cannot find parent property.");
                            return (int)ReturnValues.PropertyNotFound;
                        }

                        parentProp.Add(thisPropertyLowLevelName, newValue.Value);
                        Log($"Added Property {thisPropertyLowLevelName} with value {newValue.Value}");
                        okToSave = true;
                    }
                    else if (prop != null)// && !isAdd) //Surely just replace it if it IS there even if we said ADD it?
                    {
                        Log($"Trying to replace current property value with {newValue.Value}");
                        prop.Replace(newValue.Value);
                        okToSave = true;
                    }
                    else
                    {
                        Log($"Couldn't work out what to do!");
                        return (int)ReturnValues.OtherError;
                    }
                    if (okToSave)
                    {
                        Log("Saving changed settings");
                        File.WriteAllText(jsonFileName, settings.ToString());
                    }


                    return (int)ReturnValues.OK;
                }
                catch (Exception ex)
                {
                    Log($"ERROR! invalid file! - {ex.Message}");
                    return (int)ReturnValues.OtherError;
                }
            }
            else
            {
                //if running from VS, let's let the dev read the console...
                if (Debugger.IsAttached)
                {
                    Console.ReadKey();
                }
                return (int)ReturnValues.InvalidArgs;
            }

        }
    }
}
