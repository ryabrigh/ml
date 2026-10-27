function CallApi(request, callback, target = "", returnType = "json") {
	var jsonData = JSON.stringify(request);

	$.ajax({
		url: "https://0mwvxfy4rl.execute-api.eu-west-2.amazonaws.com/service",
		type: "POST",
		data: jsonData,
		contentType: "application/json",
		success: function (result) {
			var parsedResult = result;
			if (returnType == "json") {
				parsedResult = JSON.parse(result);
			}
			if (target == "") {
				callback(parsedResult);
			}
			else {
				callback(parsedResult, target);
			}
		},
		error: function (xhr, ajaxOptions, thrownError) {
			alert(xhr.responseText);
		}
	});
}
