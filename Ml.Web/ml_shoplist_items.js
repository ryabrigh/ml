const queryString = window.location.search;
const urlParams = new URLSearchParams(queryString);
const shopListId = urlParams.get('id');
const shopListDate = urlParams.get('date');
var selected = [];
var originalSelected = [];
var portionList = [];
var allIds = [];
var countChanges;
var rowChanged = [];

window.onload = function(){
	$("#SLItemsTitle").html("ML Shopping List - " + shopListDate);
	countChanges = 0;
	GetAll();
}

$(document).on({
    ajaxStart: function() { $("body").addClass("loading");    },
     ajaxStop: function() { $("body").removeClass("loading"); }    
});

function GetSelectedAndDeselectedData(){
	// get Deselected first
	var data = [];
/*	console.log(selected);
	console.log(originalSelected);

	console.log(rowChanged);

	rowChanged.forEach(changedId => {
		if (originalSelected.indexOf(changedId) > -1){
			console.log("found " + changedId);
			var stillSelectedIndex = selected.indexOf(changedId);
			if (stillSelectedIndex > -1) {
				// original selected and still selected so put with active = true
				data.push(GetDataFromRow(changedId, true));
				// get rid of it from selected so it isn't double counted
				selected.splice(stillSelectedIndex, 1);
			} else {
				// original selected but not still selected so put with active = false
				var baseData = GetDataFromRow(changedId, true);
				baseData["active"] = {"BOOL": false};
				data.push(baseData);
			}				
		} else {
			if (selected.indexOf(changedId) > -1){
				data.push(GetDataFromRow(changedId, false))
			}
		}
	});*/

	originalSelected.forEach(element => {
		if (rowChanged.indexOf(element.ID) > -1){
			var stillSelectedIndex = selected.indexOf(element.ID);
			if (stillSelectedIndex > -1) {
				// original selected and still selected so put with active = true
				data.push(GetDataFromRow(element.ID, true));
				// get rid of it from selected so it isn't double counted
				selected.splice(stillSelectedIndex, 1);
			} else {
				// original selected but not still selected so put with active = false
				var baseData = GetDataFromRow(element.ID, true);
				baseData["active"] = {"BOOL": false};
				data.push(baseData);
			}
		}
	});
	selected.forEach(id => {
		if (rowChanged.indexOf(id) > -1){
			data.push(GetDataFromRow(id, false))
		}
	});
	return data;
}

//		jsonData = JSON.stringify({ id: {"N": id}, name: {"S": name}, active: {"BOOL": active}, order: {"N":order}, amount: {"N":amount},
//area: {"N":area}, portion: {"N":portion}, store: {"N":store} });

function GetDataFromRow(id, origSelect){
	var thisAmount = $("#amount" + id).val();
	if (thisAmount == "") { thisAmount = "0";}
	var thisNote = $("#note" + id).val();
	var baseData =  {active: {"BOOL": true}, amount: {"N": thisAmount}, collected: {"BOOL": false},
		order: {"N": $("#listOrder" + id).html()}, portion: {"N": $("#portion" + id).val()}, product: {"N": id.toString()},
		shopList: {"N": shopListId.toString()}, note: {"S": thisNote} };
	if (origSelect) {baseData["id"] = {"N": $("#listProductId" + id).html()}; }
	return baseData;
}

/* BATCH ATTEMPT
/* if timeouts become a real problem again consider an autosave after 50 amendments
function Submit(report){
	var selectedData = GetSelectedAndDeselectedData();
	//return;
	var numBatches = Math.ceil(selectedData.length / 25);
	var doneBatches = 0;
	//console.log(numBatches);
	for (let i=0; i < numBatches; i++)
	{
		console.log("batch" + i);
		console.log(i * 25, ((i + 1) * 25));
		var thisBatch = selectedData.slice(i * 25, ((i + 1) * 25));
		console.log(thisBatch);
		var jsonData = JSON.stringify(thisBatch);
		//console.log(jsonData);
		//return;
		$.ajax({
			url: "https://i2myytdj3i.execute-api.eu-west-2.amazonaws.com/listproducts",
			type: "PUT",
			data: jsonData,
			contentType: "application/json",
			success: function(result) {
				//doneBatches++;
				//console.log("donebatch" + doneBatches);
				//if (doneBatches = numBatches){
			//		if (report == 1) { Report(); }
			//		else if (report == 2) { Print(); }
			//		else { GetAll();}
				}
				//alert("success");
			},
			error: function (xhr, ajaxOptions, thrownError) {
				alert(xhr.responseText);
			}
		});	
	}
}*/

function Submit(){
	var selectedData = GetSelectedAndDeselectedData();
	var jsonData = JSON.stringify(selectedData);
	//console.log(jsonData);
	//return;

	$.ajax({
		url: "https://i2myytdj3i.execute-api.eu-west-2.amazonaws.com/listproducts",
		type: "PUT",
		data: jsonData,
		contentType: "application/json",
		success: function(result) {
			//if (report == 1) { Report(); }
			//else if (report == 2) { Print(); } 
			//else { 
				GetAll(); 
			//}
		},
		error: function (xhr, ajaxOptions, thrownError) {
			alert(xhr.responseText);
		}
	});	
}

function GetAll(){
	selected = [];
	originalSelected = [];
	rowChanged = [];
	var sortField = "areaOrder_asc_" + shopListId; 
	$.ajax({
		url: "https://i2myytdj3i.execute-api.eu-west-2.amazonaws.com/portions",
		type: "GET",
		success: function(result) {
			for (i = 0; i < result.length; i++) {
				var thisActive = result[i]["active"];
				if (thisActive) {
					portionList.push(result[i]);
					//$('<option />', {value: result[i]["id"], text: result[i]["name"]}).appendTo($("#productPortionList"));
				}
			}
			
			//$('<option />', {value: 0, text: ""}).appendTo($("#productPortionList"));
			$.ajax({
				url: "https://i2myytdj3i.execute-api.eu-west-2.amazonaws.com/products/sort/" + sortField,
				type: "GET",
				success: function(result) {
					//console.log(result);
					//console.log(portionList);
					$("#dataTable").html("");
					var table = document.createElement("table"), row, cellA;
					table.classList.add("mltable");
					document.getElementById("dataTable").appendChild(table);
					var header = table.createTHead();
					var headerRow = header.insertRow(0);
					cellA = document.createElement("th");
					var thisFieldName = "id";
					cellA.innerHTML = thisFieldName.toUpperCase();
					cellA.classList.add("hideMobile");
					headerRow.appendChild(cellA);
					cellA = document.createElement("th");
					var thisFieldName = "name";
					cellA.innerHTML = thisFieldName.toUpperCase();
					headerRow.appendChild(cellA);
					cellA = document.createElement("th");
					var thisFieldName = "amount";
					cellA.innerHTML = thisFieldName.toUpperCase();
					cellA.style.width = "10%";
					headerRow.appendChild(cellA);
					cellA = document.createElement("th");
					var thisFieldName = "portionName";
					cellA.innerHTML = "PORTION";
					headerRow.appendChild(cellA);
					cellA = document.createElement("th");
					cellA.innerHTML = "NOTE";
					headerRow.appendChild(cellA);
					cellA = document.createElement("th");
					var thisFieldName = "areaName";
					cellA.innerHTML = "AREA";
					cellA.classList.add("hideMobile");
					headerRow.appendChild(cellA);
					/*cellA = document.createElement("th");
					var thisFieldName = "storeName";
					cellA.innerHTML = "STORE";
					headerRow.appendChild(cellA);*/

					var body = table.createTBody(0);
					for (i = 0; i < result.length; i++) {
						var thisActive = result[i]["active"];
						var thisId = result[i]["id"];
						if (thisActive) {
							allIds.push(thisId);
							row = body.insertRow();
							//row.setAttribute('onclick', 'SelectItem(' + thisId + ')');
							row.setAttribute('id', 'row' + thisId);
							var thisName;
							var thisOrder;
							var thisAmount;
							var thisPortion;
							var thisArea;
							//need to manually do it rather than iterate to get the order we need
							//for (let key in result[i]) {
							cellA = row.insertCell();
							cellA.innerHTML = thisId;
							if (result[i]["thisSl"]) {cellA.style.backgroundColor = "yellow"; selected.push(thisId);} 
							if (result[i]["listProductId"] != -1) {
								originalSelected.push({ID: thisId, ListProductId: result[i]["listProductId"]});}
							cellA.setAttribute('onclick', 'SelectItem(' + thisId + ')');
							cellA.classList.add("hideMobile");
							cellA = row.insertCell();
							thisName = result[i]["name"].trim();
							cellA.innerHTML = thisName;
							if (result[i]["thisSl"]) {cellA.style.backgroundColor = "yellow";}
							cellA.setAttribute('onclick', 'SelectItem(' + thisId + ')');
							cellA = row.insertCell();
							thisAmount = result[i]["amount"];
							//cellA.innerHTML = thisAmount;
							var input = document.createElement('input');
							input.setAttribute('id', 'amount' + thisId);
							input.setAttribute('onchange', 'rowChanged.push(' + thisId + ')');
							input.style.width = "100%";
							input.value = thisAmount;
							cellA.appendChild(input);
							//cellA.setAttribute('id', 'amount' + thisId);
							if (result[i]["thisSl"]) {cellA.style.backgroundColor = "yellow";}
							cellA = row.insertCell();
							thisPortion = result[i]["portion"];
							//cellA.innerHTML = result[i]["portion"];
							var input = document.createElement('select');
							input.setAttribute('id', 'portion' + thisId);
							input.style.width = "100%";
							//input.value = thisPortion;
							cellA.appendChild(input);
							portionList.forEach(portion => {
								$('<option />', {value: portion["id"], text: portion["name"]}).appendTo($("#portion" + thisId));
							});
							$("#portion" + thisId).val(thisPortion).change();
							input.setAttribute('onchange', 'rowChanged.push(' + thisId + ')');
							input.disabled = true;
							//cellA.setAttribute('id', 'portion' + thisId);
							if (result[i]["thisSl"]) {cellA.style.backgroundColor = "yellow";}
							cellA = row.insertCell();
							var thisNote = result[i]["note"];
							var input = document.createElement('input');
							input.setAttribute('id', 'note' + thisId);
							input.setAttribute('onchange', 'rowChanged.push(' + thisId + ')');
							input.style.width = "100%";
							input.value = thisNote;
							cellA.appendChild(input);
							cellA = row.insertCell();
							thisArea = result[i]["area"];
							cellA.innerHTML = result[i]["areaName"];
							if (result[i]["thisSl"]) {cellA.style.backgroundColor = "yellow";}
							cellA.classList.add("hideMobile");
							cellA = row.insertCell();
							thisOrder = result[i]["areaOrder"] * 1000 + result[i]["order"];
							cellA.innerHTML = thisOrder;
							cellA.style.display = "none";
							cellA.setAttribute('id', 'listOrder' + thisId);
							if (result[i]["thisSl"]) {cellA.style.backgroundColor = "yellow";}
							cellA = row.insertCell();
							thisSLid = result[i]["listProductId"];
							cellA.innerHTML = thisSLid;
							cellA.style.display = "none";
							cellA.setAttribute('id', 'listProductId' + thisId);
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

function SelectItem(id){
	//console.log(selected);
    var index = selected.indexOf(id);
    if (index > -1) { // only splice array when item is found
        selected.splice(index, 1); // 2nd parameter means remove one item only
        $("#row" + id).children('td, th').css('background-color','white');
    } else {
        selected.push(id);
        $("#row" + id).children('td, th').css('background-color','yellow');
    }
	if (rowChanged.indexOf(id) <= -1) {rowChanged.push(id);}
	//if (countChanges >= )
    //console.log(selected);
}

function Report(){
	const response = confirm("Saved all your changes?");
    if (response) {
		window.location.href = "ml_shoplist_report.html?id=" + shopListId;
	}
}

function Print(){
	const response = confirm("Saved all your changes?");
    if (response) {
		window.location.href = "ml_shoplist_print.html?id=" + shopListId;
	}
}

function SelectAll(){
	allIds.forEach(id => {
		var index = selected.indexOf(id);
		if (index <= -1) { //item is not found
			SelectItem(id);;
		}
	});
}

function Collected(){
	var updateColl = []
	$.ajax({
		url: "https://i2myytdj3i.execute-api.eu-west-2.amazonaws.com/products",
		type: "GET",
		success: function(result) {
			for (i = 0; i < result.length; i++) {
				var thisId = result[i]["id"];
				if (selected.indexOf(thisId) > -1){
					var thisAmount = parseFloat($("#amount" + thisId).val()) + parseFloat(result[i]["amount"]);
					updateColl.push({id: thisId.toString(), amount: thisAmount.toString()});
				}
			}
			var jsonData = JSON.stringify(updateColl);
			console.log(jsonData);
			//return;
			$.ajax({
				url: "https://i2myytdj3i.execute-api.eu-west-2.amazonaws.com/products/update",
				type: "PUT",
				data: jsonData,
				contentType: "application/json",
				success: function(result) {
					console.log("Products updated");
					GetAll(); 
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
