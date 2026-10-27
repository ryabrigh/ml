const queryString = window.location.search;
const urlParams = new URLSearchParams(queryString);
const mealId = urlParams.get('id');
const mealName = urlParams.get('name');
var mealType = urlParams.get('type');
var selected = [];
var originalSelected = [];
var portionList = [];
var allIds = [];
var countChanges;
var rowChanged = [];
var productNames = [];

window.onload = function(){
	$("#MLItemsTitle").html("ML Meal - " + mealName);
	countChanges = 0;
	GetAll();
}

$(document).on({
    ajaxStart: function() { $("body").addClass("loading");    },
     ajaxStop: function() { $("body").removeClass("loading"); }    
});

function GetSelectedAndDeselectedData(){
	var data = [];

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

function GetDataFromRow(id, origSelect){
	var thisAmount = $("#amount" + id).val();
	if (thisAmount == "") { thisAmount = "0";}
	var baseData =  {active: {"BOOL": true}, amount: {"N": thisAmount}, portion: {"N": $("#portion" + id).val()}, product: {"N": id.toString()},
		meal: {"N": mealId.toString()} };
	if (origSelect) {baseData["id"] = {"N": $("#mealProductId" + id).html()}; }
	return baseData;
}

function GetMealData(){
	var selectedMealTypes = "";
	for (i = 0; i < 3; i++){
		var selectName = "#mealType" + i;
		selectedMealTypes = selectedMealTypes + $(selectName).val();
	}
	mealType = selectedMealTypes;
	var baseData =  {active: {"BOOL": true}, id: {"N": mealId}, mealtypes: {"S": mealType}, name: {"S": mealName} };
	return baseData;
}
function Submit(){
	var selectedData = GetSelectedAndDeselectedData();
	var jsonData = JSON.stringify(selectedData);
	//console.log(jsonData);
	var mealData = GetMealData();
	var jsonMealData = JSON.stringify(mealData);
	//console.log(jsonMealData);
	//return;

	$.ajax({
		url: "https://i2myytdj3i.execute-api.eu-west-2.amazonaws.com/mealproducts",
		type: "PUT",
		data: jsonData,
		contentType: "application/json",
		success: function(result) {
			$.ajax({
				url: "https://i2myytdj3i.execute-api.eu-west-2.amazonaws.com/meals",
				type: "PUT",
				data: jsonMealData,
				contentType: "application/json",
				success: function(result) {
					window.location.href = "ml_meal.html?id=" + mealId + "&name=" + mealName + "&type=" + mealType;
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

function GetAll(){
	selected = [];
	originalSelected = [];
	rowChanged = [];
	productNames = [];
	var sortField = "areaOrder_asc_-1_" + mealId; 
	$.ajax({
		url: "https://i2myytdj3i.execute-api.eu-west-2.amazonaws.com/portions",
		type: "GET",
		success: function(result) {
			for (i = 0; i < result.length; i++) {
				var thisActive = result[i]["active"];
				if (thisActive) {
					portionList.push(result[i]);
				}
			}
			$.ajax({
				url: "https://i2myytdj3i.execute-api.eu-west-2.amazonaws.com/products/sort/" + sortField,
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
					cellA.style.width = "10%";
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
							allIds.push(thisId);
							productNames.push({id: thisId, name: result[i]["name"].trim().toUpperCase()});
							row = body.insertRow();
							row.setAttribute('id', 'row' + thisId);
							var thisName;
							var thisOrder;
							var thisAmount;
							var thisPortion;
							var thisArea;
							var thisStore;
							if (result[i]["thisMl"]) {
								selected.push(thisId);
							} 
							if (result[i]["mealProductId"] != -1) {
								originalSelected.push({ID: thisId, ListProductId: result[i]["mealProductId"]});}
							cellA = row.insertCell();
							thisName = result[i]["name"].trim();
							cellA.innerHTML = thisName;
							if (result[i]["thisMl"]) {cellA.style.backgroundColor = "yellow";}
							cellA.setAttribute('onclick', 'SelectItem(' + thisId + ')');
							cellA = row.insertCell();
							thisAmount = result[i]["amount"];
							var input = document.createElement('input');
							input.setAttribute('id', 'amount' + thisId);
							input.setAttribute('onchange', 'rowChanged.push(' + thisId + ')');
							input.style.width = "100%";
							input.value = thisAmount;
							cellA.appendChild(input);
							if (result[i]["thisMl"]) {cellA.style.backgroundColor = "yellow";}
							cellA = row.insertCell();
							thisPortion = result[i]["portion"];
							var input = document.createElement('select');
							input.setAttribute('id', 'portion' + thisId);
							input.style.width = "100%";
							input.disabled = true;
							cellA.appendChild(input);
							portionList.forEach(portion => {
								$('<option />', {value: portion["id"], text: portion["name"]}).appendTo($("#portion" + thisId));
							});
							$("#portion" + thisId).val(thisPortion).change();
							input.setAttribute('onchange', 'rowChanged.push(' + thisId + ')');
							if (result[i]["thisMl"]) {cellA.style.backgroundColor = "yellow";}
							cellA = row.insertCell();
							thisMLid = result[i]["mealProductId"];
							cellA.innerHTML = thisMLid;
							cellA.style.display = "none";
							cellA.setAttribute('id', 'mealProductId' + thisId);
						}
					}

					$("#dataTableSelected").html("");
					var tableSelected = document.createElement("table"), row, cellA;
					tableSelected.classList.add("mltable");
					document.getElementById("dataTableSelected").appendChild(tableSelected);
					header = tableSelected.createTHead();
					headerRow = header.insertRow(0);
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
					
					body = tableSelected.createTBody(0);
					for (i = 0; i < result.length; i++) {
						thisActive = result[i]["active"];
						var thisML = result[i]["thisMl"];
						thisId = result[i]["id"];
						if (thisActive && thisML) {
							row = body.insertRow();
							cellA = row.insertCell();
							thisName = result[i]["name"].trim();
							cellA.innerHTML = thisName;
							cellA = row.insertCell();
							thisAmount = result[i]["amount"];
							cellA.innerHTML = thisAmount;
							cellA = row.insertCell();
							var portionName = "";
							portionList.forEach(portion => {
								if (portion["id"] == result[i]["portion"]) { portionName = portion["name"];}
							});
							cellA.innerHTML = portionName
						}
					}
					var mealTypes = String(mealType).padEnd(3, '0');
					//console.log(mealTypes);
					var selectedMealTypes = [];
					for (i=0; i<3; i++){
						selectedMealTypes.push(mealTypes.substring(i, i+1))
					}
					//console.log(selectedMealTypes);
					$.ajax({
						url: "https://i2myytdj3i.execute-api.eu-west-2.amazonaws.com/mealtype",
						type: "GET",
						success: function(result) {
							//portionList = result;
							for (i=0; i < 3; i++){
								//console.log(i);
								var selectName = "#mealType" + i;
								//console.log(selectName);
								$('<option />', {value: 0, text: ""}).appendTo($(selectName));
								for (j = 0; j < result.length; j++) {
									var thisActive = result[j]["active"];
									if (thisActive) {
										$('<option />', {value: result[j]["id"], text: result[j]["name"]}).appendTo($(selectName));
									}
								}
								if (selectedMealTypes[i] != 0){
									$(selectName).val(selectedMealTypes[i]).change();
								}
							}
						},
						error: function (xhr, ajaxOptions, thrownError) {
							alert(xhr.responseText);
						}
					});

					$("table td").addClass("mlcell");
					$("table th").addClass("mlheadercell");				},
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
    var index = selected.indexOf(id);
    if (index > -1) { // only splice array when item is found
        selected.splice(index, 1); // 2nd parameter means remove one item only
        $("#row" + id).children('td, th').css('background-color','white');
    } else {
        selected.push(id);
        $("#row" + id).children('td, th').css('background-color','yellow');
    }
	if (rowChanged.indexOf(id) <= -1) {rowChanged.push(id);}
}

function Parse(){
	var badChars = [",", ":", "(", ")"];
	var mealDetails = $("#mealParse").val().replace(/(?:\r\n|\r|\n)/g, ' ');
	badChars.forEach(badChar => {
		mealDetails = mealDetails.split(badChar).join(" ");
	});
	var multiWordItems = [];
	var startIndex = -1;
	var endIndex = -1;
	for (i=0; i <= mealDetails.length; i++){
		if (mealDetails.substring(i,i+1)== "["){
			startIndex = i + 1;
		}
		if (mealDetails.substring(i,i+1)== "]"){
			endIndex = i;
			multiWordItems.push(mealDetails.substring(startIndex, endIndex).toUpperCase());
		}
	}
	//console.log(multiWordItems);
	//return;

	items = mealDetails.toUpperCase().split(" ");
	var twoWordItems = [];
	for (i=0; i < items.length - 1; i++){
		twoWordItems.push(items[i] + " " + items[i+1]);
	}
	//console.log(twoWordItems);

	items.push(...multiWordItems);
	items.push(...twoWordItems);
	items = WithSynonyms(items);
	//console.log(items);
	//return;
	productNames.forEach(product => {
		if (items.indexOf(product.name) > -1){
			//console.log("found " + product.name);
			if (selected.indexOf(product.id) == -1){
				SelectItem(product.id);
			}
		}
	});
	Submit();
}

function WithSynonyms(items){
	//console.log(items);
	var returnItems = items;
	synonyms.forEach(synonym => {
		//console.log(synonym);
		var pushedSynonym = false;
		synonym.forEach(thisItem => {
			//console.log(thisItem);
			if (items.indexOf(thisItem) > -1){
				//console.log(items);
				//console.log("found " + thisItem);
				if (!pushedSynonym){
					returnItems.push(...synonym);
					pushedSynonym = true;
				}
			}
		});
	});
	return returnItems;
}

const synonyms = [["ONION", "ONIONS", "BROWN ONIONS"],
["CHEESE", "CHEDDAR", "CHEDDAR CHEESE"],
["EGGS", "EGG"],
["FLOUR", "PLAIN FLOUR"],
["MARGARINE", "LURPAK"],
["CREAM", "SINGLE CREAM"],
["VEGETABLE STOCK", "VEGETABLE STOCK CUBES"],
["CARROT","CARROTS"],
["BEEF STOCK","BEEF STOCK POTS"],
["BASIL","FRESH BASIL"],
["SPAGHETTI","DRY SPAGHETTI NOT QUICK COOK"],
["LEMON","LEMONS"],
["SHALLOT","SHALLOTS"],
["CHIVES","FRESH CHIVES"],
["LARGE TUNA","LARGE TUNAS", "TIN OF TUNA"],
["SAUSAGE","SAUSAGES"],
["CHICKEN STOCK","CHICKEN STOCK POTS"],
["POTATOES", "MARIS PIPER POTATOES", "MARIS PIPER", "POTATO"],
["CRAB", "CRAB MEAT"],
["NUT OIL", "GROUNDNUT OIL"],
["NOODLES", "DRY EGG NOODLES MEDIUM"],
["PENNE", "DRY PENNE PASTA"],
["GRUYERE CHEESE", "GRUYÈRE CHEESE"],
["ANCHOVIES", "ANCHOVY FILLETS"],
["BABY POTATOES", "SALAD POTATOES"],
["PARSLEY", "FRESH PARSLEY"],
["MAYO", "MAYONNAISE"],
["BAKING SODA", "BICARBONATE OF SODA"],
["TOMATO PUREE", "TOMATO PURÉE"],
["MOZZARELLA", "GRATED MOZZARELLA LARGE"],
["FETA", "FETA CHEESE"],
["VEG MINCE", "VEG MINCE VIVERA"],
["BASMATI", "BASMATI RICE"],
["COCONUT MILK", "LIGHT COCONUT MILK"],
["LEEK", "LEEKS"],
["CREME FRAICHE", "LOW FAT CREME FRAICHE"],
["PLAIN YOGHURT 0% SMALL", "YOGURT", "YOGHURT"],
["CANNELLONI", "CANNELLONI PASTA"]];
