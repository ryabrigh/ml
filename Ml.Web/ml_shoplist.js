window.onload = function(){
	GetAll();
	Clear();
}


$(document).on({
    ajaxStart: function() { $("body").addClass("loading");    },
     ajaxStop: function() { $("body").removeClass("loading"); }    
});

function Clear(){
	$("#shopListId").val("");
	$("#shopListDate").val("");
	$("#shopListActive").prop('checked', false);
}

function AddEdit(){
	var id = $("#shopListId").val();
	var slDate = $("#shopListDate").val();
	var active = $("#shopListActive")[0].checked;
	if (slDate == "") { alert("You have to enter a date"); return;}
	SaveData(id, slDate, active);
}

function SaveData(id, slDate, active){
	var jsonData;
	if (id == ""){
		jsonData = JSON.stringify({ listDate: {"S": slDate} });
	} else {
		jsonData = JSON.stringify({ id: {"N": id}, listDate: {"S": slDate}, active: {"BOOL": active} });
	}
	
	//console.log(jsonData);
	// when we call python, it isn't enough to just stringify the fields
	// we have to put the data type in there too
	// so not just id: id, but id: {"N": id} and "S" for strings
	$.ajax({
		url: "https://i2myytdj3i.execute-api.eu-west-2.amazonaws.com/shoplists",
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
		url: "https://i2myytdj3i.execute-api.eu-west-2.amazonaws.com/shoplists/" + id,
		type: "GET",
		success: function(result) {
			if (result == ""){	
				alert("No shopList found for ID " + id);
			} else {
				$("#shopListId").val(result.id.N);
				$("#shopListDate").val(result.listDate.S);
				$("#shopListActive").prop('checked', result.active.BOOL);
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
		url: "https://i2myytdj3i.execute-api.eu-west-2.amazonaws.com/shoplists",
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
			cellA.innerHTML = "DATE";
			headerRow.appendChild(cellA);
			cellA = document.createElement("th");
			cellA.style.width = "5%";
			headerRow.appendChild(cellA);
			cellA = document.createElement("th");
			cellA.style.width = "5%";
			headerRow.appendChild(cellA);
			
			var body = table.createTBody(0);
			for (i = 0; i < result.length; i++) {
				var thisActive = result[i]["active"];
				if (showInactive || thisActive) {
					row = body.insertRow();
					var thisId;
					var thisDate;
					cellA = row.insertCell();
					thisId = result[i]["id"];
					//console.log(result[i]["listDate"].trim());
					cellA.setAttribute('onclick', 'GoToListPage(' + thisId + ',"' + result[i]["listDate"].trim() + '")');
					cellA.onmouseover = function() {
						this.style.backgroundColor = "lightgrey";
					}
					cellA.onmouseout = function() {
						this.style.backgroundColor = "white";
					}
					cellA.innerHTML = thisId;
					if (!thisActive) {cellA.style.backgroundColor = "lightgrey";}
					cellA = row.insertCell();
					thisDate = result[i]["listDate"].trim();
					cellA.innerHTML = thisDate;
					if (!thisActive) {cellA.style.backgroundColor = "lightgrey";}
					cellA = row.insertCell();
					cellA.style.width = "10%";
					var icon = thisActive ? "trash" : "plus";
					cellA.innerHTML = '<button type="button" onclick="SaveData(\'' + thisId + '\', \'' + thisDate + 
					'\', ' + !thisActive + ')" class="btn btn-default">' +
					'<span class="glyphicon glyphicon-' + icon + '" /></button>';
					cellA = row.insertCell();
					cellA.style.width = "10%";
					var icon = "remove";
					cellA.innerHTML = '<button type="button" onclick="DeleteItem(\'' + thisId + '\')" class="btn btn-default">' +
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
		url: "https://i2myytdj3i.execute-api.eu-west-2.amazonaws.com/shoplists/" + id,
		type: "DELETE",
		success: function(result) {
			GetAll();
		},
		error: function (xhr, ajaxOptions, thrownError) {
			//console.log(xhr);
			if (xhr.status == 503)
			{
				// assume a gateway timeout
				alert("Gateway timeout. Check in a few minutes to see if data has been deleted.")
			} else {
				alert(xhr.responseText);
			}
		}
	});
}

function GoToListPage(id,listDate){
	//console.log(listDate);
	window.location.href = "ml_shoplist_items.html?id=" + id + "&date=" + listDate;
}
