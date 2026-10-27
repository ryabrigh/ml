var rowChanged = [];
var originalSelected = [];

window.onload = function(){
	GetAll();
}

$(document).on({
    ajaxStart: function() { $("body").addClass("loading");    },
     ajaxStop: function() { $("body").removeClass("loading"); }    
});

function GetSelectedAndDeselectedData(){
	var data = [];
	rowChanged.forEach(id => {
		var existingId = -1;
		var existing = originalSelected.filter(s => s["ID"] == id);
		if (existing.length > 0) { existingId = existing[0]["depletionId"]};
		var thisAmount = $("#amount" + id).val();
		if (thisAmount == "") { thisAmount = "0";}
		var baseData = {active: {"BOOL": true}, depletion: {"N": thisAmount}, product: {"N": id.toString()}};
		if (existingId != -1) {baseData["id"] = {"N": existingId.toString()}; }
		data.push(baseData);
	});
	return data;
}

function Submit(){
	var selectedData = GetSelectedAndDeselectedData();
	var jsonData = JSON.stringify(selectedData);
	//console.log(jsonData);
	$.ajax({
		url: "https://i2myytdj3i.execute-api.eu-west-2.amazonaws.com/depletion",
		type: "PUT",
		data: jsonData,
		contentType: "application/json",
		success: function(result) {
			GetAll(); 
		},
		error: function (xhr, ajaxOptions, thrownError) {
			alert(xhr.responseText);
		}
	});	
}

function GetAll(){
	rowChanged = [];
	var depletionlist = [];
	$.ajax({
		url: "https://i2myytdj3i.execute-api.eu-west-2.amazonaws.com/depletion",
		type: "GET",
		success: function(result) {
			//console.log(result);
			for (i = 0; i < result.length; i++) {
				var thisActive = result[i]["active"];
				if (thisActive) {
					depletionlist.push(result[i]);
				}
			}
			$.ajax({
				url: "https://i2myytdj3i.execute-api.eu-west-2.amazonaws.com/products/sort/name_asc",
				type: "GET",
				success: function(result) {
					$("#dataTable").html("");
					var table = document.createElement("table"), row, cellA;
					table.classList.add("mltable");
					document.getElementById("dataTable").appendChild(table);
					var header = table.createTHead();
					var headerRow = header.insertRow(0);
					cellA = document.createElement("th");
					var thisFieldName = "name";
					cellA.innerHTML = thisFieldName.toUpperCase();
					headerRow.appendChild(cellA);
					cellA = document.createElement("th");
					var thisFieldName = "amount";
					cellA.innerHTML = thisFieldName.toUpperCase();
					headerRow.appendChild(cellA);
					cellA = document.createElement("th");
					var thisFieldName = "portionName";
					cellA.innerHTML = "PORTION";
					headerRow.appendChild(cellA);

					var body = table.createTBody(0);
					for (i = 0; i < result.length; i++) {
						var thisActive = result[i]["active"];
						var thisId = result[i]["id"];
						if (thisActive) {
							var existingDepletion = depletionlist.filter(function (item) {
								return item.product == thisId;
							});
							var thisAmount = 0;
							if (existingDepletion.length > 0){
								//console.log(existingDepletion);
								originalSelected.push({ID: thisId, depletionId: existingDepletion[0]["id"]});
								thisAmount = existingDepletion[0]["depletion"];
							}
							row = body.insertRow();
							cellA = row.insertCell();
							var thisName = result[i]["name"].trim();
							cellA.innerHTML = thisName;
							cellA.setAttribute('onclick', 'SelectItem(' + thisId + ')');
							cellA = row.insertCell();
							var input = document.createElement('input');
							input.setAttribute('id', 'amount' + thisId);
							input.setAttribute('onchange', 'rowChanged.push(' + thisId + ')');
							input.style.width = "100%";
							input.value = thisAmount;
							cellA.appendChild(input);
							cellA = row.insertCell();
							var thisPortion = result[i]["portionName"];
							cellA.innerHTML = thisPortion;
						}
					}
					$("table td").addClass("mlcell");
					$("table th").addClass("mlheadercell");
				},
				error: function (xhr, ajaxOptions, thrownError) {
					alert(xhr.responseText);
				}
			});
				},
		error: function (xhr, ajaxOptions, thrownError) {
			alert(xhr.responseText);
		}
	});
}