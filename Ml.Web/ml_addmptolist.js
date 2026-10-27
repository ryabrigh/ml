var shopListSelected = -1;
var mealPlanSelected = -1;
var allshoplistids = [];
var allmealplanids = [];
//var allMealPlans = [];
var portionList = [];
var productList = [];
var mealList = [];
var mealProductList = [];

window.onload = function(){
	GetShoppingLists();
	GetMealPlans();
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
	//console.log(shopListSelected, mealPlanSelected);
	if (shopListSelected == -1){
		alert("You have to pick a shopping list");
		return;
	}
	if (mealPlanSelected == -1){
		alert("You have to pick a meal plan");
		return;
	}
	var subtract = confirm("Subtract inventory from required amounts? OK for yes, Cancel for No");

	//var sortField = "areaOrder_asc_-1_" + mealId; 
	$.ajax({
		url: "https://i2myytdj3i.execute-api.eu-west-2.amazonaws.com/mealproducts",
		type: "GET",
		success: function(result) {
			for (i = 0; i < result.length; i++) {
				var thisActive = result[i]["active"];
				if (thisActive) {
					mealProductList.push(result[i]);
				}
			}
			$.ajax({
				url: "https://i2myytdj3i.execute-api.eu-west-2.amazonaws.com/mealplans/" + mealPlanSelected,
				type: "GET",
				success: function(result) {
					var listOfProductsInMP = [];
					for (i = 0; i < result.length; i++) {
						var thisActive = result[i]["active"];
						var thisId = result[i]["id"];
						if (thisActive) {
							var thisMealProducts = mealProductList.filter(m => m["meal"] == result[i]["meal"]);
							thisMealProducts.forEach(mp => {
								listOfProductsInMP.push({meal: mp["mealName"], amount: mp["amount"],
									product: mp["productName"], portion: mp["portionName"]
								});
							});
						}
					}
					var listOfProductNames = [];
					var runningCount = [];
					var conflicts = [];
					var listOfProductsInConflict = [];

					//console.log(listOfProductsInMP);
					listOfProductsInMP.forEach(product => {
						var productName = product["product"];
						var index = listOfProductNames.indexOf(productName);
						if (index > -1){
							if (runningCount[index]["portion"] != product["portion"]){
								// conflict
								if (listOfProductsInConflict.indexOf(productName) == -1){
									listOfProductsInConflict.push(productName);
									conflicts.push("Conflict: " + product["product"]);
									var listOfProductsForThisProduct = listOfProductsInMP.filter(p => p["product"] == product["product"]);
									listOfProductsForThisProduct.forEach(t => {
										conflicts.push(" - " + t["meal"] + " " + t["amount"] + " " + t["portion"]);
									});
								}
							} else {
								// no conflict add amount
								runningCount[index]["amount"] = runningCount[index]["amount"] + product["amount"];
								runningCount[index]["amount"] = Math.round(runningCount[index]["amount"] * 100) / 100;
							}
						} else {
							listOfProductNames.push(productName);
							runningCount.push({product: product["product"], amount: product["amount"], portion: product["portion"]});
						}
					});
					
					//console.log(runningCount);
					if (conflicts.length > 0) { console.log(conflicts); alert("conflicts - check console log"); }
					$.ajax({
						url: "https://i2myytdj3i.execute-api.eu-west-2.amazonaws.com/products/sort/areaOrder_asc_-1",
						type: "GET",
						success: function(result) {
							//console.log(runningCount);
							//console.log(result);
							var listproducts = [];			
							runningCount.forEach(needed => {
								var productInInventory = result.filter(r => r["name"] == needed["product"])[0];
								var subtractAmount = 0;
								if (subtract) { subtractAmount = parseFloat(productInInventory["amount"]); }
								var requiredAmount = parseFloat(needed["amount"]) - subtractAmount;
								//console.log(needed);
								//console.log(productInInventory);
								//console.log(requiredAmount);
								if (requiredAmount > 0) {
									listproducts.push({amount: {"N": requiredAmount.toString()}, order: {"N": productInInventory["order"].toString()},
										portion: {"N": productInInventory["portion"].toString()}, product: {"N": productInInventory["id"].toString()},
										shopList: {"N": shopListSelected.toString()}, collected: {"BOOL": false},
										note: {"S": ""}});
								}
							});
							//console.log(listproducts);
							var jsonData = JSON.stringify(listproducts);
							$.ajax({
								url: "https://i2myytdj3i.execute-api.eu-west-2.amazonaws.com/listproducts",
								type: "PUT",
								data: jsonData,
								contentType: "application/json",
								success: function(result) {
									alert("done");
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
		
					// TODO: ajax get products (inventory)
					// go through runningcount list and subtract amounts in inventory
					// if <= zero left then remove from running count
					// products put those items where running count amount > 0
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

function GetShoppingLists(){
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
			
			var body = table.createTBody(0);
			for (i = 0; i < result.length; i++) {
				var thisActive = result[i]["active"];
				if (thisActive) {
					var thisId = result[i]["id"];
					allshoplistids.push(thisId);
					row = body.insertRow();
					row.setAttribute('id', 'slrow' + thisId);
					row.setAttribute('onclick', 'SelectShoplist(' + thisId + ')');
					var thisDate;
					cellA = row.insertCell();
					//console.log(result[i]["listDate"].trim());
					cellA.onmouseover = function() {
						this.style.backgroundColor = "lightgrey";
					}
					cellA.onmouseout = function() {
						this.style.backgroundColor = "white";
					}
					cellA.innerHTML = thisId;
					cellA = row.insertCell();
					thisDate = result[i]["listDate"].trim();
					cellA.innerHTML = thisDate;
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

function GetMealPlans(){
	$.ajax({
		url: "https://i2myytdj3i.execute-api.eu-west-2.amazonaws.com/plans",
		type: "GET",
		success: function(result) {
			//allMealPlans = result;
			$("#dataTable2").html("");
			var table = document.createElement("table"), row, cellA;
			table.classList.add("mltable");
			document.getElementById("dataTable2").appendChild(table);
			var header = table.createTHead();
			var headerRow = header.insertRow(0);
			cellA = document.createElement("th");
			cellA.innerHTML = "ID";
			headerRow.appendChild(cellA);
			cellA = document.createElement("th");
			cellA.innerHTML = "START DATE";
			headerRow.appendChild(cellA);
			
			var body = table.createTBody(0);
			for (i = 0; i < result.length; i++) {
				var thisActive = result[i]["active"];
				if (thisActive) {
					var thisId = result[i]["id"];
					allmealplanids.push(thisId);
					row = body.insertRow();
					row.setAttribute('id', 'mprow' + thisId);
					row.setAttribute('onclick', 'SelectMealPlan(' + thisId + ')');
					cellA = row.insertCell();
					cellA.onmouseover = function() {
						this.style.backgroundColor = "lightgrey";
					}
					cellA.onmouseout = function() {
						this.style.backgroundColor = "white";
					}
					cellA.innerHTML = thisId;
					cellA = row.insertCell();
					var thisDate = result[i]["startDate"].trim();
					cellA.innerHTML = thisDate;
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

function SelectShoplist(id){
	//console.log(id);
	if (shopListSelected == id)
	{
		// deselected
		shopListSelected = -1;
	} else {
		shopListSelected = id;
	}
	allshoplistids.forEach(id => {
		if (shopListSelected == id){
			$("#slrow" + id).children('td, th').css('background-color','yellow');
		} else {
			$("#slrow" + id).children('td, th').css('background-color','white');
		}
	});
}

function SelectMealPlan(id){
	//console.log(id);
	if (mealPlanSelected == id)
	{
		// deselected
		mealPlanSelected = -1;
	} else {
		mealPlanSelected = id;
	}
	allmealplanids.forEach(id => {
		if (mealPlanSelected == id){
			$("#mprow" + id).children('td, th').css('background-color','yellow');
		} else {
			$("#mprow" + id).children('td, th').css('background-color','white');
		}
	});
}
