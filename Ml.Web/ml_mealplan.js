window.onload = function(){
	GetAll();
	Clear();
}


$(document).on({
    ajaxStart: function() { $("body").addClass("loading");    },
     ajaxStop: function() { $("body").removeClass("loading"); }    
});

function Clear(){
	$("#mealPlanId").val("");
	$("#mealPlanStartDate").val("");
	$("#mealPlanActive").prop('checked', false);
}

function AddEdit(){
	var id = $("#mealPlanId").val();
	var mpDate = $("#mealPlanStartDate").val();
	var active = $("#mealPlanActive")[0].checked;
	if (mpDate == "") { alert("You have to enter a date"); return;}
	SaveData(id, mpDate, active);
}

function SaveData(id, mpDate, active){
	var jsonData;
	if (id == ""){
		jsonData = JSON.stringify({ startDate: {"S": mpDate} });
	} else {
		jsonData = JSON.stringify({ id: {"N": id}, startDate: {"S": mpDate}, active: {"BOOL": active} });
	}
	$.ajax({
		url: "https://i2myytdj3i.execute-api.eu-west-2.amazonaws.com/plans",
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
		url: "https://i2myytdj3i.execute-api.eu-west-2.amazonaws.com/plans/" + id,
		type: "GET",
		success: function(result) {
			if (result == ""){	
				alert("No meal plan found for ID " + id);
			} else {
				$("#mealPlanId").val(result.id.N);
				$("#mealPlanStartDate").val(result.startDate.S);
				$("#mealPlanActive").prop('checked', result.active.BOOL);
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
		url: "https://i2myytdj3i.execute-api.eu-west-2.amazonaws.com/plans",
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
			cellA.innerHTML = "START DATE";
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
					cellA.setAttribute('onclick', 'GoToPlanPage(' + thisId + ',"' + result[i]["startDate"].trim() + '")');
					cellA.onmouseover = function() {
						this.style.backgroundColor = "lightgrey";
					}
					cellA.onmouseout = function() {
						this.style.backgroundColor = "white";
					}
					cellA.innerHTML = thisId;
					if (!thisActive) {cellA.style.backgroundColor = "lightgrey";}
					cellA = row.insertCell();
					thisDate = result[i]["startDate"].trim();
					cellA.innerHTML = thisDate;
					if (!thisActive) {cellA.style.backgroundColor = "lightgrey";}
					cellA = row.insertCell();
					cellA.style.width = "10%";
					var icon = thisActive ? "trash" : "plus";
					cellA.innerHTML = '<button type="button" onclick="SaveData(\'' + thisId + '\', \'' + thisDate + 
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
		url: "https://i2myytdj3i.execute-api.eu-west-2.amazonaws.com/plans/" + id,
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

function GoToPlanPage(id,startDate){
	//console.log(listDate);
	window.location.href = "ml_mealplan_items.html?id=" + id + "&date=" + startDate;
}
