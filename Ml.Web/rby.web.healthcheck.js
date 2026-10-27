function LoadHealthCheck() {
	GetHC();
}

$(document).on({
    ajaxStart: function() { $("body").addClass("loading");    },
     ajaxStop: function() { $("body").removeClass("loading"); }    
});

function GetHC() {
	var healthCheckDate = formatDateDDMMYYYY(new Date());
	var serviceData = { "requestName": "jsHC " + healthCheckDate } ;
	var request = { "serviceType": "HealthCheck", "serviceData": serviceData };
	if (local) {
		ShowHealthCheckData("");
	}
	else {
		CallApi(request, ShowHealthCheckData);
	}
}

function ShowHealthCheckData(result) {
	var data;
	if (local) {
		var data = LocalTestHealthCheckData();
	}
	else {
		console.log(result);
		data = result;
	}
	$("#hc").html(data.replace(/\n/g, "<br>"));
}
