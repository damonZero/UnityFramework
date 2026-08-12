var csInterface

var gExtensionID;

// some events we are interested in
var eventMake = 1298866208; // "Mk  "
var eventDelete = 1147958304; // "Dlt " 
var eventClose = 1131180832; // "Cls " 
var eventSelect = 1936483188; // "slct" 
var eventSet = 1936028772; // "setd" 

function initEvents(cs) {
    csInterface = cs
    // Get extension ID
    gExtensionID = csInterface.getExtensionID();
    addEvents();
}

function addEvents() {
    // alert('addEvents')
    try {
        var event = new CSEvent("com.adobe.PhotoshopRegisterEvent", "APPLICATION");
        event.data = [eventMake, eventDelete, eventClose, eventSelect, eventSet].toString();
        event.extensionId = gExtensionID;
        csInterface.dispatchEvent(event);

        csInterface.addEventListener("com.adobe.PhotoshopJSONCallback" + gExtensionID, function (csEvent) {
            if (typeof csEvent.data === "string") {
                var eventData = csEvent.data.replace("ver1,{", "{");
                var eventDataParse = JSON.parse(eventData);
                var jsonStringBack = JSON.stringify(eventDataParse);
                if (eventDataParse.eventID === eventSelect) {
                    var info = eventDataParse.eventData;
                    var layerID = info.layerID
                    var name = info.null._name
                    $('#selectLayer').val(name);
                    return;
                }
            }
            $('#selectLayer').val('');
        });
    } catch (error) {
        alert(error)
    }
}