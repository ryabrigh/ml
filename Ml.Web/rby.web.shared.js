const local = false;

function CallApi(request, callback) {
	var jsonData = JSON.stringify(request);

	$.ajax({
		url: "https://szxp9svq7a.execute-api.eu-west-2.amazonaws.com/service",
		type: "POST",
		data: jsonData,
		contentType: "application/json",
		success: function (result) {
			callback(result);
		},
		error: function (xhr, ajaxOptions, thrownError) {
			alert(xhr.responseText);
		}
	});	
}
function loadHTML(url, targetId, callback) {
	const target = document.getElementById(targetId);
	if (!target) {
		console.error(`Element with ID "${targetId}" not found.`);
		return;
	}

	fetch(url, {cache: "no-store"})
		.then(response => {
			if (!response.ok) {
				throw new Error(`HTTP error! Status: ${response.status}`);
			}
			return response.text();
		})
		.then(html => {
			target.innerHTML = html; // Insert HTML into the div
			if (typeof callback === 'function') {
				callback(); // Trigger "onload" equivalent
			}
		})
		.catch(err => {
			console.error('Error loading HTML:', err);
			target.innerHTML = `<p style="color:red;">Failed to load content.</p>`;
		});
}

// A function that accepts another function as a parameter
function executeCallback(callback, value) {
	//console.log(value);
	// Validate that the argument is actually a function
	if (typeof callback !== 'function') {
		console.error('Error: callback must be a function');
		return;
	}

	//console.log('Executing callback...');
	callback(value); // Call the passed function
}

function CreateTable(data, fields, rowclick) {
	var table = document.createElement("table"), row, cellA;
	var header = CreateTableHeader(table, fields);
	var body = CreateTableBody(table, data, fields, rowclick);
	return table;
}

function CreateTableHeader(table, fields) {
	var header = table.createTHead();
	var headerRow = header.insertRow(0);
	for (j = 0; j < fields.length; j++) {
		var keyField = "";
		var underscore = fields[j].indexOf("_");
		var content = "";
		var formatWidth = "";
		var formatClass = "";
		if (underscore > -1) {
			content = fields[j].substring(underscore + 1);
			// formatting here based on what we put before the underscore
			var formats = fields[j].substring(0, underscore).split(":");
			for (i = 0; i < formats.length; i++) {
				switch (formats[i].substring(0, 1)) {
					case "L":
						content = formats[i].substring(1);
						break;
					case "K":
						keyField = fields[j].substring(underscore + 1);
						break;
					case "W":
						formatWidth = formats[i].substring(1) + "%";
						break;
					case "C":
						formatClass = formats[i].substring(1);
						break;
					default:
					// default code block
				}
			}
		}
		else {
			content = fields[j];
		}

		if (keyField == "") {
			cellA = document.createElement("th");
			if (content != "") {
				cellA.innerHTML = content.toUpperCase();
			}
			if (formatWidth != "") {
				cellA.style.width = formatWidth;
			}
			if (formatClass != "") {
				cellA.classList.add(formatClass);
			}
			headerRow.appendChild(cellA);
		}
	}
	return header;
}

function CreateTableBody(table, data, fields, rowclick) {
	//console.log(rowclick);
	// identify any key fields up front
	var keyField = "";
	var formatWidth = [];
	var formatClass = [];
	for (i = 0; i < fields.length; i++) {
		// initialise format objects
		formatWidth[i] = "";
		formatClass[i] = "";

		var underscore = fields[i].indexOf("_");
		if (underscore > -1) {
			// formatting here based on what we put before the underscore
			var formats = fields[i].substring(0, underscore).split(":");
			for (k = 0; k < formats.length; k++) {
				switch (formats[k].substring(0, 1)) {
					case "K":
						keyField = fields[i].substring(underscore + 1);
						break;
					case "W":
						formatWidth[i] = formats[k].substring(1) + "%";
						break;
					case "C":
						formatClass[i] = formats[k].substring(1);
						break;
					default:
					// default code block
				}
			}
		}
	}

	//console.log(formatWidth);

	var body = table.createTBody(0);
	var keyFieldData = "";
	var keyFieldCounter = 0;
	var thisId = 0;
	for (i = 0; i < data.length; i++) {
		thisId = data[i]["Id"];
		row = body.insertRow();
		if (keyField != "" && keyFieldData != data[i][keyField]) {
			cellA = row.insertCell();
			keyFieldData = data[i][keyField];
			cellA.innerHTML = keyFieldData;
			keyFieldCounter++;
			cellA.id = "keyField" + keyFieldCounter;
			row = body.insertRow();
		}
		if (rowclick != "") {
			row.setAttribute('onclick', rowclick + '(' + thisId + ')');
		}
		for (j = 0; j < fields.length; j++) {
			var fieldName = "";
			var underscore = fields[j].indexOf("_");
			var urlField = false;
			if (underscore > -1) {
				fieldName = fields[j].substring(underscore + 1);
				var formats = fields[j].substring(0, underscore).split(":");
				for (k = 0; k < formats.length; k++) {
					switch (formats[k].substring(0, 1)) {
						case "U":
							urlField = true;
							break;
						default:
						// default code block
					}
				}
			}
			else {
				fieldName = fields[j];
			}
			if (keyField == "" ||  fieldName != keyField) {
				cellA = row.insertCell();
				if (fieldName != "") {
					cellA.innerHTML = data[i][fieldName];
				}
				if (formatWidth[j] != "") {
					cellA.style.width = formatWidth[j];
				}
				if (formatClass[j] != "") {
					cellA.classList.add(formatClass[j]);
				}
				if (urlField && data[i][fieldName] != "") {
					//console.log("urlfield:" + fieldName);
					cellA.setAttribute('onclick', fieldName + 'Click(' + thisId + ')');
				}
			}
		}
	}
	return body;
}

function formatDateDDMMYYYY(date) {
	if (!(date instanceof Date) || isNaN(date)) {
		throw new Error("Invalid Date object provided.");
	}

	const day = String(date.getDate()).padStart(2, '0');       // Ensure 2 digits
	const month = String(date.getMonth() + 1).padStart(2, '0'); // Months are 0-based
	const year = date.getFullYear();

	return `${day}/${month}/${year}`;
}

function toLongDate(inputDate) {
	try {
		// Ensure we have a Date object
		const dateObj = (inputDate instanceof Date) ? inputDate : new Date(inputDate);

		// Validate the date
		if (isNaN(dateObj.getTime())) {
			console.error("Invalid date provided.");
			return null;
		}

		// Format to long date
		return dateObj.toLocaleDateString("en-GB", {
			year: "numeric",
			month: "long",
			day: "numeric"
		});
	} catch (err) {
		console.error("Error formatting date:", err);
		return null;
	}
}
