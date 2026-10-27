window.onload = function(){
	GetAll();
	Clear();
}


$(document).on({
    ajaxStart: function() { $("body").addClass("loading");    },
     ajaxStop: function() { $("body").removeClass("loading"); }    
});

function Clear(){
	$("#areaId").val("");
	$("#areaName").val("");
	$("#areaOrder").val("");
	$("#areaActive").prop('checked', false);
}

function AddEdit(){
	var id = $("#areaId").val();
	var name = $("#areaName").val();
	var active = $("#areaActive")[0].checked;
	var order = $("#areaOrder").val();
	if (name == "") { alert("You have to enter a name"); return;}
	if (order == "") { alert("You have to enter an order"); return;}
	SaveData(id, name, active, order);
}

function SaveData(id, name, active, order){
	var jsonData;
	if (id == ""){
		jsonData = JSON.stringify({ name: {"S": name}, order: {"N":order} });
	} else {
		jsonData = JSON.stringify({ id: {"N": id}, name: {"S": name}, active: {"BOOL": active}, order: {"N":order} });
	}
	
	//console.log(jsonData);
	// when we call python, it isn't enough to just stringify the fields
	// we have to put the data type in there too
	// so not just id: id, but id: {"N": id} and "S" for strings
	$.ajax({
		url: "https://i2myytdj3i.execute-api.eu-west-2.amazonaws.com/areas",
		type: "PUT",
		data: jsonData,
		contentType: "application/json",
		success: function(result) {
			GetAll();
			Clear();
		},
		error: function (xhr, ajaxOptions, thrownError) {
			alert(xhr.responseText);
		}
	});
}

function GetItem(id){
	$.ajax({
		url: "https://i2myytdj3i.execute-api.eu-west-2.amazonaws.com/areas/" + id,
		type: "GET",
		success: function(result) {
			if (result == ""){	
				alert("No area found for ID " + id);
			} else {
				$("#areaId").val(result.id.N);
				$("#areaName").val(result.name.S);
				$("#areaOrder").val(result.order.N);
				$("#areaActive").prop('checked', result.active.BOOL);
			}
		},
		error: function (xhr, ajaxOptions, thrownError) {
			alert(xhr.responseText);
		}
	});
}

function GetAll(){
	var showInactive = $("#showInactive")[0].checked;
	$.ajax({
		url: "https://i2myytdj3i.execute-api.eu-west-2.amazonaws.com/areas",
		type: "GET",
		success: function(result) {
			$("#dataTable").html("");
			var table = document.createElement("table"), row, cellA;
			table.classList.add("mltable");
			document.getElementById("dataTable").appendChild(table);
			var header = table.createTHead();
			var headerRow = header.insertRow(0);
			//for (let key in result[0]) {
			//	if (key != "active"){
				cellA = document.createElement("th");
				cellA.innerHTML = "ID";
				headerRow.appendChild(cellA);
				cellA = document.createElement("th");
				cellA.innerHTML = "NAME";
				headerRow.appendChild(cellA);
				cellA = document.createElement("th");
				cellA.innerHTML = "ORDER";
				headerRow.appendChild(cellA);
//	}
			//}
			cellA = document.createElement("th");
			headerRow.appendChild(cellA);
			
			var body = table.createTBody(0);
			for (i = 0; i < result.length; i++) {
				var thisActive = result[i]["active"];
				if (showInactive || thisActive) {
					row = body.insertRow();
					var thisId;
					var thisName;
					var thisOrder;
					//need to manually do it rather than iterate to get the order we need
					//for (let key in result[i]) {
						//if (key != "active"){
							cellA = row.insertCell();
							thisId = result[i]["id"];
							//if (key == "id") {
								//thisId = thisVal;
								cellA.setAttribute('onclick', 'GetItem(' + thisId + ')');
								cellA.onmouseover = function() {
									this.style.backgroundColor = "lightgrey";
								}
								cellA.onmouseout = function() {
									this.style.backgroundColor = "white";
								}
								cellA.innerHTML = thisId;
								if (!thisActive) {cellA.style.backgroundColor = "lightgrey";}
								//} 
							cellA = row.insertCell();
							thisName = result[i]["name"].trim();
							cellA.innerHTML = thisName;
							if (!thisActive) {cellA.style.backgroundColor = "lightgrey";}
							//if (key == "name") { thisName = thisVal.trim();}
							cellA = row.insertCell();
							thisOrder = result[i]["order"];
							cellA.innerHTML = thisOrder;
							if (!thisActive) {cellA.style.backgroundColor = "lightgrey";}
							//if (key == "order") { thisOrder = thisVal;}
							//cellA.innerHTML = thisVal;
							if (!thisActive) {cellA.style.backgroundColor = "lightgrey";}
						//}
					//}
					cellA = row.insertCell();
					cellA.style.width = "10%";
					var icon = thisActive ? "trash" : "plus";
					cellA.innerHTML = '<button type="button" onclick="SaveData(\'' + thisId + '\', \'' + thisName + 
					'\', ' + !thisActive + ',\'' + thisOrder + '\')" class="btn btn-default">' +
					'<span class="glyphicon glyphicon-' + icon + '" /></button>';
				}
			}
			$("table td").addClass("mlcell");
			$("table th").addClass("mlheadercell");
		},
		error: function (xhr, ajaxOptions, thrownError) {
			alert(xhr.responseText);
		}
	});
}

function DeleteItem(id){
	$.ajax({
		url: "https://i2myytdj3i.execute-api.eu-west-2.amazonaws.com/areas/" + id,
		type: "DELETE",
		success: function(result) {
			GetAll();
		},
		error: function (xhr, ajaxOptions, thrownError) {
			alert(xhr.responseText);
		}
	});
}
