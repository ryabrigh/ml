window.onload = function(){
	LoadLists();
	GetAll();
	Clear();
}

function LoadLists(){
	$.ajax({
		url: "https://i2myytdj3i.execute-api.eu-west-2.amazonaws.com/portions",
		type: "GET",
		success: function(result) {
			portionList = result;
			$('<option />', {value: 0, text: ""}).appendTo($("#productPortionList"));
			for (i = 0; i < result.length; i++) {
				var thisActive = result[i]["active"];
				if (thisActive) {
					$('<option />', {value: result[i]["id"], text: result[i]["name"]}).appendTo($("#productPortionList"));
				}
			}
		},
		error: function (xhr, ajaxOptions, thrownError) {
			alert(xhr.responseText);
		}
	});
	$.ajax({
		url: "https://i2myytdj3i.execute-api.eu-west-2.amazonaws.com/areas",
		type: "GET",
		success: function(result) {
			portionList = result;
			$('<option />', {value: 0, text: ""}).appendTo($("#productAreaList"));
			for (i = 0; i < result.length; i++) {
				var thisActive = result[i]["active"];
				if (thisActive) {
					$('<option />', {value: result[i]["id"], text: result[i]["name"]}).appendTo($("#productAreaList"));
				}
			}
		},
		error: function (xhr, ajaxOptions, thrownError) {
			alert(xhr.responseText);
		}
	});
	$.ajax({
		url: "https://i2myytdj3i.execute-api.eu-west-2.amazonaws.com/stores",
		type: "GET",
		success: function(result) {
			portionList = result;
			$('<option />', {value: 0, text: ""}).appendTo($("#productStoreList"));
			for (i = 0; i < result.length; i++) {
				var thisActive = result[i]["active"];
				if (thisActive) {
					$('<option />', {value: result[i]["id"], text: result[i]["name"]}).appendTo($("#productStoreList"));
				}
			}
		},
		error: function (xhr, ajaxOptions, thrownError) {
			alert(xhr.responseText);
		}
	});
}

$(document).on({
    ajaxStart: function() { $("body").addClass("loading");    },
     ajaxStop: function() { $("body").removeClass("loading"); }    
});

function Clear(){
	$("#productId").val("");
	$("#productName").val("");
	$("#productOrder").val("");
	$("#productAmount").val("");
	$("#productAreaList").val("");
	$("#productPortionList").val("");
	$("#productStoreList").val("");
	$("#productActive").prop('checked', false);
}

function AddEdit(){
	var id = $("#productId").val();
	var name = $("#productName").val();
	var active = $("#productActive")[0].checked;
	var order = $("#productOrder").val();
	var amount = $("#productAmount").val();
	var area = $("#productAreaList").val();
	var portion = $("#productPortionList").val();
	var store = $("#productStoreList").val();
	if (name == "") { alert("You have to enter a name"); return;}
	if (amount == "") { alert("You have to enter an amount"); return;}
	if (portion == "" || portion == 0) { alert("You have to enter an portion"); return;}
	if (area == "" || area == 0) { alert("You have to enter an area"); return;}
	if (order == "") { alert("You have to enter an order"); return;}
	if (store == "" || store == 0) { alert("You have to enter an store"); return;}
	SaveData(id, name, active, order, amount, area, portion, store);
}

function SaveData(id, name, active, order, amount, area, portion, store){
	var jsonData;

	if (id == ""){
		jsonData = JSON.stringify({ name: {"S": name}, order: {"N":order}, amount: {"N":amount}, area: {"N":area}, portion: {"N":portion},
		store: {"N":store} });
	} else {
		jsonData = JSON.stringify({ id: {"N": id}, name: {"S": name}, active: {"BOOL": active}, order: {"N":order}, amount: {"N":amount},
		 area: {"N":area}, portion: {"N":portion}, store: {"N":store} });
	}
	
	$.ajax({
		url: "https://i2myytdj3i.execute-api.eu-west-2.amazonaws.com/products",
		type: "PUT",
		data: jsonData,
		contentType: "application/json",
		success: function(result) {
			//GetAll();
			Clear();
		},
		error: function (xhr, ajaxOptions, thrownError) {
			alert(xhr.responseText);
		}
	});
}

function GetItem(id){
	$.ajax({
		url: "https://i2myytdj3i.execute-api.eu-west-2.amazonaws.com/products/" + id,
		type: "GET",
		success: function(result) {
			if (result == ""){	
				alert("No product found for ID " + id);
			} else {
				$("#productId").val(result.id.N);
				$("#productName").val(result.name.S);
				$("#productOrder").val(result.order.N);
				$("#productActive").prop('checked', result.active.BOOL);
				$("#productAmount").val(result.amount.N);
				$("#productAreaList").val(result.area.N).change();
				$("#productPortionList").val(result.portion.N).change();
				$("#productStoreList").val(result.store.N).change();
			}
		},
		error: function (xhr, ajaxOptions, thrownError) {
			alert(xhr.responseText);
		}
	});
}

function GetAll(){
	var showInactive = $("#showInactive")[0].checked;
	var sortField = $("#sortField").html(); 
	var sortItems = sortField.split("_");
	var sortIcon = " <span class='glyphicon glyphicon-triangle-" + (sortItems[1] == "desc" ? "bottom" : "top") + "'></span>" 
	//console.log(sortIcon);
	$.ajax({
		url: "https://i2myytdj3i.execute-api.eu-west-2.amazonaws.com/products/sort/" + sortField,
		type: "GET",
		success: function(result) {
			//console.log(result);
			$("#dataTable").html("");
			var table = document.createElement("table"), row, cellA;
			table.classList.add("mltable");
			document.getElementById("dataTable").appendChild(table);
			var header = table.createTHead();
			var headerRow = header.insertRow(0);
			cellA = document.createElement("th");
			var thisFieldName = "id";
			cellA.setAttribute('onclick', 'SetSortField("' + thisFieldName + '")');
			cellA.innerHTML = thisFieldName.toUpperCase() + (sortItems[0] == thisFieldName ? sortIcon : "") ;
			headerRow.appendChild(cellA);
			cellA = document.createElement("th");
			var thisFieldName = "name";
			cellA.setAttribute('onclick', 'SetSortField("' + thisFieldName + '")');
			cellA.innerHTML = thisFieldName.toUpperCase() + (sortItems[0] == thisFieldName ? sortIcon : "");
			headerRow.appendChild(cellA);
			cellA = document.createElement("th");
			var thisFieldName = "amount";
			cellA.setAttribute('onclick', 'SetSortField("' + thisFieldName + '")');
			cellA.innerHTML = thisFieldName.toUpperCase() + (sortItems[0] == thisFieldName ? sortIcon : "");
			headerRow.appendChild(cellA);
			cellA = document.createElement("th");
			var thisFieldName = "portionName";
			cellA.setAttribute('onclick', 'SetSortField("' + thisFieldName + '")');
			cellA.innerHTML = "PORTION" + (sortItems[0] == thisFieldName ? sortIcon : "");
			headerRow.appendChild(cellA);
			cellA = document.createElement("th");
			var thisFieldName = "areaName";
			cellA.setAttribute('onclick', 'SetSortField("' + thisFieldName + '")');
			cellA.innerHTML = "AREA" + (sortItems[0] == thisFieldName ? sortIcon : "");
			headerRow.appendChild(cellA);
			cellA = document.createElement("th");
			var thisFieldName = "order";
			cellA.setAttribute('onclick', 'SetSortField("' + thisFieldName + '")');
			cellA.innerHTML = thisFieldName.toUpperCase() + (sortItems[0] == thisFieldName ? sortIcon : "");
			headerRow.appendChild(cellA);
			cellA = document.createElement("th");
			var thisFieldName = "storeName";
			cellA.setAttribute('onclick', 'SetSortField("' + thisFieldName + '")');
			cellA.innerHTML = "STORE" + (sortItems[0] == thisFieldName ? sortIcon : "");
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
					var thisOrder;
					var thisAmount;
					var thisPortion;
					var thisArea;
					var thisStore;
					//need to manually do it rather than iterate to get the order we need
					//for (let key in result[i]) {
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
					thisAmount = result[i]["amount"];
					cellA.innerHTML = thisAmount;
					if (!thisActive) {cellA.style.backgroundColor = "lightgrey";}
					cellA = row.insertCell();
					thisPortion = result[i]["portion"];
					cellA.innerHTML = result[i]["portionName"];
					if (!thisActive) {cellA.style.backgroundColor = "lightgrey";}
					cellA = row.insertCell();
					thisArea = result[i]["area"];
					cellA.innerHTML = result[i]["areaName"];;
					if (!thisActive) {cellA.style.backgroundColor = "lightgrey";}
					cellA = row.insertCell();
					thisOrder = result[i]["order"];
					cellA.innerHTML = thisOrder;
					if (!thisActive) {cellA.style.backgroundColor = "lightgrey";}
					cellA = row.insertCell();
					thisStore = result[i]["store"];
					cellA.innerHTML = result[i]["storeName"];
					if (!thisActive) {cellA.style.backgroundColor = "lightgrey";}
					cellA = row.insertCell();
					cellA.style.width = "10%";
					var icon = thisActive ? "trash" : "plus";
					cellA.innerHTML = '<button type="button" onclick="SaveData(\'' + thisId + '\', \'' + thisName + 
					'\', ' + !thisActive + ',\'' + thisOrder + '\', \'' + thisAmount + '\', \'' + thisArea + '\', \'' + thisPortion + '\', \'' +
					thisStore + '\')" class="btn btn-default">' + '<span class="glyphicon glyphicon-' + icon + '" /></button>';
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
//id, name, active, order, amount, area, portion, store
function DeleteItem(id){
	$.ajax({
		url: "https://i2myytdj3i.execute-api.eu-west-2.amazonaws.com/products/" + id,
		type: "DELETE",
		success: function(result) {
			GetAll();
		},
		error: function (xhr, ajaxOptions, thrownError) {
			alert(xhr.responseText);
		}
	});
}

function SetSortField(field){
	var sortItems = $("#sortField").html().split("_");
	if (field == sortItems[0]) {
		var sortDir = (sortItems[1] == "asc" ? "desc" : "asc");
		//console.log(field + "_" + sortDir);
		$("#sortField").html(field + "_" + sortDir);
	} else {
		$("#sortField").html(field + "_asc");
	}

	GetAll();
}