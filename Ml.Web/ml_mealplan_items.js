const queryString = window.location.search;
const urlParams = new URLSearchParams(queryString);
const mealPlanId = urlParams.get('id');
const mealPlanDate = urlParams.get('date');
var selected = [];
var originalSelected = [];
var mealList = [];
var allIds = [];
var countChanges;
var mealChanged = [];
var defaultDays = 7;
var defaultSections = 4;
var grid = [];
const MealTypeSections = ["6","5","4","7"];

window.onload = function(){
	$("#MPItemsTitle").html("ML Meal Plan - starts " + mealPlanDate);
	countChanges = 0;
	selected = [];
	GetAll();
	LoadMealTypesList();
	LoadTemplateTable();
}

$(document).on({
    ajaxStart: function() { $("body").addClass("loading");    },
     ajaxStop: function() { $("body").removeClass("loading"); }    
});

function GetChangedData(){
	var data = [];

	mealChanged.forEach(change => {
		var baseData = GetDataFromBlock(change[0], change[1]);
		if (baseData != "") { data.push(baseData); }
	});
	return data;
}

function GetDataFromBlock(day, section){
	var selectedMeal = $("#meal_" + day + "_" + section).val();
	var baseData =  {day: {"N": (day + 1).toString()}, section: {"N": (section + 1).toString()}, meal: {"N": selectedMeal.toString()},
		plan: {"N": mealPlanId}};
	var thisGrid = grid[day][section];
	if (thisGrid != 0) {
		// original selection
		if (selectedMeal == 0) {
			// deselected to nothing
			baseData["meal"] = {"N": thisGrid.meal.toString()};
			baseData["active"] = {"BOOL": false};
		} else {
			baseData["active"] = {"BOOL": true};
		}
		baseData["id"] = {"N": thisGrid.id.toString()};
	} else {
		if (selectedMeal == 0) {
			// not original selection but no meal chosen so ignore
			return "";
		}
	}
	return baseData;
}

function Submit(){
	var changedData = GetChangedData();
	//console.log(changedData);
	if (changedData.length == 0){ console.log("no changes made"); return; }
	var jsonData = JSON.stringify(changedData);
	//console.log(jsonData);

	$.ajax({
		url: "https://i2myytdj3i.execute-api.eu-west-2.amazonaws.com/mealplans",
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

	Date.prototype.addDays = function (num) {
		var value = this.valueOf();
		value += 86400000 * num;
		return new Date(value);
	}

	selected = [];
	originalSelected = [];
	rowChanged = [];
	$.ajax({
		url: "https://i2myytdj3i.execute-api.eu-west-2.amazonaws.com/meals",
		type: "GET",
		success: function(result) {
			for (i = 0; i < result.length; i++) {
				var thisActive = result[i]["active"];
				if (thisActive) {
					mealList.push(result[i]);
				}
			}
			
			$.ajax({
				url: "https://i2myytdj3i.execute-api.eu-west-2.amazonaws.com/mealplans/" + mealPlanId,
				type: "GET",
				success: function(result) {
					//console.log(result);
					$("#dataTable").html("");
					var table = document.createElement("table"), row, cellA;
					table.classList.add("mealplantable");
					document.getElementById("dataTable").appendChild(table);
					
					grid = [];
					for (i = 0; i < defaultDays; i++){
						var row = [];
						for (j = 0; j < defaultSections; j++){
							row.push(0);
						}
						grid.push(row);
					}

					var body = table.createTBody(0);
					for (i = 0; i < result.length; i++) {
						var thisActive = result[i]["active"];
						var thisId = result[i]["id"];
						if (thisActive) {
							var thisDay = parseInt(result[i]["day"]);
							var thisSection = parseInt(result[i]["section"]);
							grid[thisDay - 1][thisSection - 1] = {meal: result[i]["meal"], mealName: result[i]["mealName"], id: thisId };
						}
					}
					
					//console.log(grid);
					for (i = 0; i < defaultDays; i++){
						row = body.insertRow();
						cellA = row.insertCell();
						var dateParts = mealPlanDate.split("/");
						var thisDate = new Date(+dateParts[2], dateParts[1] - 1, +dateParts[0]);
						thisDate.setDate(thisDate.getDate() + i);
						cellA.innerHTML = thisDate.toDateString();
						for (j = 0; j < defaultSections; j++){
							//var block = (i * 7) + j;
							cellA = row.insertCell();
							var input = document.createElement('select');
							input.setAttribute('id', 'meal_' + i + "_" + j);
							input.style.width = "100%";
							cellA.appendChild(input);
							$('<option />', {value: 0, text: ""}).appendTo($("#meal_" + i + "_" + j));
							var mealsForThisSection = mealList.filter(m => m["mealtypes"].includes(MealTypeSections[j]));
							mealsForThisSection.forEach(meal => {
								$('<option />', {value: meal["id"], text: meal["name"]}).appendTo($("#meal_" + i + "_" + j));
							});
							if (grid[i][j] != 0){
								var thisMeal = parseInt(grid[i][j].meal);
								$("#meal_" + i + "_" + j).val(thisMeal).change();
							} 
							input.setAttribute('onchange', 'SelectItem([' + i + ',' + j + '])');
						}
					}
					$("table td").addClass("mealplancell");
					$("table th").addClass("mealplanheadercell");
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

function SelectItem(block){
	var thisSelected = (block[0] * defaultDays) + block[1];
	if (selected.indexOf(thisSelected) == -1)
	{
		mealChanged.push(block);
		selected.push(thisSelected);
	}
}

function Random(){
	//console.log(selected);
	for (i=0; i<7; i++){
		for (j=0; j<4; j++){
			var blockSelected = $("#meal_" + i + "_" + j).val();
			//console.log(blockSelected);
			//var blockSelected = selected.indexOf(2);
			//console.log(blockSelected);
			if (blockSelected == 0) {
				//var selectedIndex = Math.floor(Math.random() * mealList.length);
				//console.log(i,j);
				var selectedMeal = GetRandomMeal(i,j);
				//selectedMeal = "X";
				//console.log(j,selectedMeal);
				if (selectedMeal != "X"){
					$("#meal_" + i + "_" + j).val(selectedMeal["id"]).change();
				}
			}
		}
	}
}

function GetRandomMeal(i,j){
	var templateValue = $("#template_" + i + "_" + j).val();
	//console.log(templateValue);
	if (templateValue == "" || templateValue == "X" || templateValue == "x") { return "X";}
	//console.log(templateValue);
	var filteredMeals = [];
	mealList.forEach(meal => {
		var include = true;
		var thisMealTypes = meal["mealtypes"];
		for (k=0; k < templateValue.length; k++){
			if (!thisMealTypes.includes(templateValue[k])){
				include = false;
			}
		}
		if (include){
			filteredMeals.push(meal);
		}
	});
	//console.log(filteredMeals);
	//var mealsForThisSection = mealList.filter(m => m["mealtypes"].includes(templateValue));
	//console.log(mealsForThisSection);
	var selectedIndex = Math.floor(Math.random() * filteredMeals.length);
	var selectedMeal = filteredMeals[selectedIndex];
	return selectedMeal;
}

function ComplexInclude(dbmealTypes, templateVal){
	var include = true;
	for (i=0; i < templateVal.length; i++){
		if (!dbmealTypes.includes(templateVal[i])){
			include = false;
		}
	}
	return include;
}

function LoadMealTypesList(){
	$.ajax({
		url: "https://i2myytdj3i.execute-api.eu-west-2.amazonaws.com/mealtype",
		type: "GET",
		success: function(result) {
//			console.log(result);
			$("#listOfMealtypes").html("");
			var label = document.createElement('label');
			label.innerHTML = "Mealtypes:";
			$("#listOfMealtypes").append(label);
			var br = document.createElement('br');
			$("#listOfMealtypes").append(br);
			label = document.createElement('label');
			label.innerHTML = "X - do not pick";
			$("#listOfMealtypes").append(label);
			br = document.createElement('br');
			$("#listOfMealtypes").append(br);
			for (i = 0; i < result.length; i++) {
				var thisActive = result[i]["active"];
				if (thisActive) {
					label = document.createElement('label');
					label.innerHTML = result[i]["id"] + " - " + result[i]["name"];
					$("#listOfMealtypes").append(label);
					br = document.createElement('br');
					$("#listOfMealtypes").append(br);
				}
			}	 
		},
		error: function (xhr, ajaxOptions, thrownError) {
			alert(xhr.responseText);
		}
	});	
}

function LoadTemplateTable(){
	$("#templateTable").html("");
	var label = document.createElement('label');
	label.innerHTML = "Template:";
	$("#templateTable").append(label);
	var br = document.createElement('br');
	$("#templateTable").append(br);
var table = document.createElement("table"), row, cellA;
	table.classList.add("mltable");
	document.getElementById("templateTable").appendChild(table);
	var body = table.createTBody(0);
	
	for (i = 0; i < defaultDays; i++){
		row = body.insertRow();
		for (j = 0; j < defaultSections; j++){
			cellA = row.insertCell();
			var input = document.createElement('input');
			input.setAttribute('id', 'template_' + i + "_" + j);
			input.style.width = "100%";
			input.value = j == 3 ? "X" : MealTypeSections[j];
			cellA.appendChild(input);
		}
	}
	$("table td").addClass("mlcell");
	$("table th").addClass("mlheadercell");
}