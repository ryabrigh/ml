const queryString = window.location.search;
const urlParams = new URLSearchParams(queryString);
const shopListId = urlParams.get('id');
var selected = [];
var taken = [];

window.onload = function(){
	GetAll();
}

$(document).on({
    ajaxStart: function() { $("body").addClass("loading");    },
     ajaxStop: function() { $("body").removeClass("loading"); }    
});

function GetAll(){
	var sortField = "areaOrder_asc_" + shopListId; 
	$.ajax({
		url: "https://i2myytdj3i.execute-api.eu-west-2.amazonaws.com/products/sort/" + sortField,
		type: "GET",
		success: function(result) {
			//console.log(result[1]);
			$("#dataTable").html("");
			$("#dataTable2").html("");
			var table = document.createElement("table"), row, cellA;
			table.classList.add("mltable");
			document.getElementById("dataTable").appendChild(table);
			var table2 = document.createElement("table"), row, cellA;
			table2.classList.add("mltable");
			document.getElementById("dataTable2").appendChild(table2);
			
			var currArea = "";
			var count = [0,0];
			var thisCount = 0;
			var area1 = [];
			var area2 = [];
			for (i = 0; i < result.length; i++) {
				var thisActive = result[i]["active"];
				var thisSL = result[i]["thisSl"];
				if (thisActive && thisSL) {
					thisArea = result[i]["area"];
					if (currArea == "") {currArea = thisArea; console.log(result[i]["areaName"]);}
					if (currArea != "" && currArea != thisArea)
					{
						count[thisCount]++;
						var otherCount = thisCount == 0 ? 1 : 0;
						if (thisCount == 1) {area2.push(currArea);} else {area1.push(currArea);}
						if (count[thisCount] >= count[otherCount]){
							thisCount = thisCount == 0 ? 1 : 0;
						}
					}
					else {
						count[thisCount]++;
					}
					currArea = thisArea;
				}
			}
			if (thisCount == 1) {area2.push(currArea);} else {area1.push(currArea);}

			var body = table.createTBody(0);
			var body2 = table2.createTBody(0);
			var currArea = "";

			var thisBody;
			for (i = 0; i < result.length; i++) {
				var thisActive = result[i]["active"];
				var thisSL = result[i]["thisSl"];
                var thisId = result[i]["id"];
				if (thisActive && thisSL) {
					selected.push(thisId);
					thisArea = result[i]["area"];
					if (currArea != "" && currArea != thisArea){
						row = thisBody.insertRow();
						cellA = row.insertCell();
						cellA.innerHTML = '<label>';
					}
					currArea = thisArea;

					var thisBody = area1.indexOf(thisArea) > -1 ? body : body2;

					row = thisBody.insertRow();
					row.setAttribute('onclick', 'SelectItem(' + thisId + ')');
					row.setAttribute('id', 'row' + thisId);
					var thisName;
					//var thisAmount;
					//var thisPortion;
					var thisArea;
					//need to manually do it rather than iterate to get the order we need
					cellA = row.insertCell();
					thisName = result[i]["name"].trim();
					//thisAmount = result[i]["amount"];
					//thisPortion = result[i]["portion"];
					var thisNote = result[i]["note"];
					cellA.innerHTML = thisName + " " + thisNote // + " " + result[i]["portionName"];
					//cellA.setAttribute('id', 'portion' + thisId);
					/*cellA = row.insertCell();
					cellA.innerHTML = result[i]["areaName"];
					cellA.setAttribute('id', 'area' + thisId);
					cellA.style.display = "none";
					cellA = row.insertCell();
					thisOrder = result[i]["areaOrder"] * 1000 + result[i]["order"];
					cellA.innerHTML = thisOrder;
					cellA.style.display = "none";
					cellA.setAttribute('id', 'listOrder' + thisId);
					cellA = row.insertCell();
					thisStore = result[i]["store"];
					cellA.innerHTML = result[i]["storeName"];
					cellA.style.display = "none";
					cellA = row.insertCell();
					thisSLid = result[i]["listProductId"];
					cellA.innerHTML = thisSLid;
					cellA.style.display = "none";
					cellA.setAttribute('id', 'listProductId' + thisId);*/
					row.classList.add(result[i]["areaName"].replace(/\s/g, ''));
					if (result[i]["storeName"] == "Saino") { row.classList.add("Saino");}
					}
			}
			$("table td").addClass("reportcell");
			$("table th").addClass("mlheadercell");
//			selected.forEach(id => {
//				console.log($("#row" + id).style.backgroundColor);
//				$("#row" + id).children('td, th').css('background-color', AreaColour($("#area" + id).html()).Main);
//			});		
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
        $("#row" + id).addClass("Selected");
    } else {
        selected.push(id);
        $("#row" + id).removeClass("Selected");
    }
}