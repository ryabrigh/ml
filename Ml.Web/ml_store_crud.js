window.onload = function(){
	GetAll();
	Clear();
}


$(document).on({
    ajaxStart: function() { $("body").addClass("loading");    },
     ajaxStop: function() { $("body").removeClass("loading"); }    
});

function Clear(){
	$("#storeId").val("");
	$("#storeName").val("");
	$("#storeActive").prop('checked', false);
}

function AddEdit(){
	var id = $("#storeId").val();
	var name = $("#storeName").val();
	var active = $("#storeActive")[0].checked;
	if (name == "") { alert("You have to enter a name"); return;}
	SaveData(id, name, active);
}

function SaveData(id, name, active){
	var jsonData;
	if (id == ""){
		jsonData = JSON.stringify({ name: {"S": name} });
	} else {
		jsonData = JSON.stringify({ id: {"N": id}, name: {"S": name}, active: {"BOOL": active} });
	}
	
	//console.log(jsonData);
	// when we call python, it isn't enough to just stringify the fields
	// we have to put the data type in there too
	// so not just id: id, but id: {"N": id} and "S" for strings
	$.ajax({
		url: "https://i2myytdj3i.execute-api.eu-west-2.amazonaws.com/stores",
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
		url: "https://i2myytdj3i.execute-api.eu-west-2.amazonaws.com/stores/" + id,
		type: "GET",
		success: function(result) {
			if (result == ""){	
				alert("No store found for ID " + id);
			} else {
				$("#storeId").val(result.id.N);
				$("#storeName").val(result.name.S);
				$("#storeActive").prop('checked', result.active.BOOL);
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
		url: "https://i2myytdj3i.execute-api.eu-west-2.amazonaws.com/stores",
		type: "GET",
		success: function(result) {
			$("#dataTable").html("");
			var table = document.createElement("table"), row, cellA;
			table.classList.add("mltable");
			document.getElementById("dataTable").appendChild(table);
			var header = table.createTHead();
			var headerRow = header.insertRow(0);
			cellA = document.createElement("th");
			cellA.innerHTML = "ID";
			headerRow.appendChild(cellA);
			cellA = document.createElement("th");
			cellA.innerHTML = "NAME";
			headerRow.appendChild(cellA);
			cellA = document.createElement("th");
			headerRow.appendChild(cellA);
			
			var body = table.createTBody(0);
			for (i = 0; i < result.length; i++) {
				var thisActive = result[i]["active"];
				if (showInactive || thisActive) {
					row = body.insertRow();
					var thisId;
					var thisName;
					cellA = row.insertCell();
					thisId = result[i]["id"];
					cellA.setAttribute('onclick', 'GetItem(' + thisId + ')');
					cellA.onmouseover = function() {
						this.style.backgroundColor = "lightgrey";
					}
					cellA.onmouseout = function() {
						this.style.backgroundColor = "white";
					}
					cellA.innerHTML = thisId;
					if (!thisActive) {cellA.style.backgroundColor = "lightgrey";}
					cellA = row.insertCell();
					thisName = result[i]["name"].trim();
					cellA.innerHTML = thisName;
					if (!thisActive) {cellA.style.backgroundColor = "lightgrey";}
					cellA = row.insertCell();
					cellA.style.width = "10%";
					var icon = thisActive ? "trash" : "plus";
					cellA.innerHTML = '<button type="button" onclick="SaveData(\'' + thisId + '\', \'' + thisName + 
					'\', ' + !thisActive + ')" class="btn btn-default">' +
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
		url: "https://i2myytdj3i.execute-api.eu-west-2.amazonaws.com/stores/" + id,
		type: "DELETE",
		success: function(result) {
			GetAll();
		},
		error: function (xhr, ajaxOptions, thrownError) {
			alert(xhr.responseText);
		}
	});
}
