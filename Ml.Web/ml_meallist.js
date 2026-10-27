window.onload = function(){
	GetAll();
	Clear();
}


$(document).on({
    ajaxStart: function() { $("body").addClass("loading");    },
     ajaxStop: function() { $("body").removeClass("loading"); }    
});

function Clear(){
	$("#mealId").val("");
	$("#mealName").val("");
	$("#mealActive").prop('checked', false);
}

function AddEdit(){
	var id = $("#mealId").val();
	var mName = $("#mealName").val();
	var active = $("#mealActive")[0].checked;
	if (mName == "") { alert("You have to enter a name"); return;}
	SaveData(id, mName, active);
}

function SaveData(id, mName, active){
	var jsonData;
	if (id == ""){
		jsonData = JSON.stringify({ name: {"S": mName} });
	} else {
		jsonData = JSON.stringify({ id: {"N": id}, name: {"S": mName}, active: {"BOOL": active} });
	}
	
	$.ajax({
		url: "https://i2myytdj3i.execute-api.eu-west-2.amazonaws.com/meals",
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
		url: "https://i2myytdj3i.execute-api.eu-west-2.amazonaws.com/meals/" + id,
		type: "GET",
		success: function(result) {
			if (result == ""){	
				alert("No meal found for ID " + id);
			} else {
				$("#mealId").val(result.id.N);
				$("#mealName").val(result.name.S);
				$("#mealActive").prop('checked', result.active.BOOL);
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
		url: "https://i2myytdj3i.execute-api.eu-west-2.amazonaws.com/meals",
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
					thisName = result[i]["name"].trim();
					thisType = result[i]["mealtypes"];
					cellA.setAttribute('onclick', 'GoToMealPage(' + thisId + ',"' + thisName + '","' + thisType + '")');
					cellA.onmouseover = function() {
						this.style.backgroundColor = "lightgrey";
					}
					cellA.onmouseout = function() {
						this.style.backgroundColor = "white";
					}
					cellA.innerHTML = thisId;
					if (!thisActive) {cellA.style.backgroundColor = "lightgrey";}
					cellA = row.insertCell();
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
		url: "https://i2myytdj3i.execute-api.eu-west-2.amazonaws.com/meals/" + id,
		type: "DELETE",
		success: function(result) {
			GetAll();
		},
		error: function (xhr, ajaxOptions, thrownError) {
			alert(xhr.responseText);
		}
	});
}

function GoToMealPage(id, name, type){
	//console.log(listDate);
	window.location.href = "ml_meal.html?id=" + id + "&name=" + name + "&type=" + type;
}
