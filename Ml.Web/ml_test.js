window.onload = function(){
	StartTests();
}

$(document).on({
    ajaxStart: function() { $("body").addClass("loading");    },
     ajaxStop: function() { $("body").removeClass("loading"); }    
});

function StartTests() {
	var serviceData = "";
	var request = { "serviceType": "Exist", "serviceData": "" };
	CallApi(request, ShowResultsText);

	var serviceData = { "Crud": "RA", "Id": 0, "TableName": "ShopList", "CrudData": {}, "Model": "ShopList" };
	request = { "serviceType": "CRUD", "serviceData": serviceData };
	CallApi(request, ShowResultsJson);
}

function ShowResultsText(result) {
	console.log(result);
	$("#testOutput").append(result);
}

function ShowResultsJson(result) {
	console.log(result);
	var parsed = JSON.parse(result);
	$("#testOutput").append(parsed);
}
