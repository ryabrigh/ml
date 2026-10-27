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
			$("#dataTable").html("");
			var table = document.createElement("table"), row, cellA;
			table.classList.add("mltable");
			document.getElementById("dataTable").appendChild(table);
			
			var body = table.createTBody(0);
			for (i = 0; i < result.length; i++) {
				var thisActive = result[i]["active"];
				var thisSL = result[i]["thisSl"];
                var thisId = result[i]["id"];
				if (thisActive && thisSL) {
					selected.push(thisId);
					row = body.insertRow();
					row.setAttribute('onclick', 'SelectItem(' + thisId + ')');
					row.setAttribute('id', 'row' + thisId);
					var thisName;
					var thisAmount;
					var thisPortion;
					//need to manually do it rather than iterate to get the order we need
					cellA = row.insertCell();
					thisName = result[i]["name"].trim();
					//thisAmount = result[i]["amount"];
					//thisPortion = result[i]["portion"];
					var thisNote = result[i]["note"];
					cellA.innerHTML = thisName + " " + thisNote // + " " + result[i]["portionName"];
					row.classList.add(result[i]["areaName"].replace(/\s/g, ''));
					if (result[i]["storeName"] == "Saino") { row.classList.add("Saino");}
					}
			}
			$("table td").addClass("reportcell");
			$("table th").addClass("mlheadercell");
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
        $("#row" + id).addClass("Selected");
    } else {
        selected.push(id);
        $("#row" + id).removeClass("Selected");
    }
}