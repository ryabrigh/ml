window.onload = function(){
	StartTests();
}


$(document).on({
    ajaxStart: function() { $("body").addClass("loading");    },
     ajaxStop: function() { $("body").removeClass("loading"); }    
});


function CallApi(request, callback) {
	var jsonData = JSON.stringify(request);

	$.ajax({
		url: "https://0mwvxfy4rl.execute-api.eu-west-2.amazonaws.com/service",
		type: "POST",
		data: jsonData,
		contentType: "application/json",
		success: function (result) {
			callback(result);
		},
		error: function (xhr, ajaxOptions, thrownError) {
			alert(xhr.responseText);
		}
	});
}

function StartTests() {
	var request = { "serviceType": "Exist", "serviceData": "" };
	CallApi(request, ShowResults);
}

function ShowResults(result) {
	console.log(result);
	$("#testOutput").append(result);
}
