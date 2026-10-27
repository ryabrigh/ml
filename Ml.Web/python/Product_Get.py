import json
import boto3

#def RelationalItems():
#    return {"portion", "area", "store"}
    
#def RelationalGet(prop, id):
#    client = boto3.client('dynamodb')
#    prop=prop.capitalize()
#    data = client.get_item(TableName=prop, Key={"id": {"N": str(id)}})
#    if "Item" in data:
#        return data["Item"]["name"]["S"]
#    else:
#        return ""
    
def SimpleDict(origDict):
    client = boto3.client('dynamodb')

    portions = client.scan(
        TableName='Portion'
    )

    areas = client.scan(
        TableName='Area'
    )

    stores = client.scan(
        TableName='Store'
    )

    newDict = []
    for item in origDict:
        newItem = {}
        for prop in item:
            propVal = item[prop].get("N")
            if propVal == None:
                propVal = item[prop].get("S")
            else:
                if '.' in propVal:
                    propVal = float(propVal)
                else:
                    propVal = int(propVal)
                    #maybe an id field?
                    if prop=="portion":
                        propItem = next((p for p in portions["Items"] if p["id"].get("N") == str(propVal)), "")
                        propName = propItem["name"].get("S")
                    elif prop=="area":
                        propItem = next((a for a in areas["Items"] if a["id"].get("N") == str(propVal)), "")
                        propName = propItem["order"].get("N")
                        newItem["areaOrder"] = propName
                        propName = propItem["name"].get("S")
                    elif prop=="store":
                        propItem = next((s for s in stores["Items"] if s["id"].get("N") == str(propVal)), "")
                        propName = propItem["name"].get("S")
                    else:
                        propName = ""

                    if propName != "":
                        newItem[prop + "Name"] = propName
            if propVal == None:
                propVal = item[prop].get("BOOL")
            newItem[prop] = propVal
        newDict.append(newItem)
    return newDict
    
def lambda_handler(event, context):
    client = boto3.client('dynamodb')

    try:
        item = event["pathParameters"]
        paramField = item["sort"].split("_")
        sortfield = paramField[0]
        sortorder = paramField[1]
    except Exception as error:
        sortfield = "id"
        sortorder = "asc"
        paramField = ""
    
    data = client.scan(
        TableName='Product'
        )
    
    paramLength = len(paramField);
    if paramLength == 3:
        shopList = int(paramField[2])
    else:
        shopList = -1
        
    if paramLength == 4:
        meal = int(paramField[3])
    else:
        meal = -1
    
        
    if "Items" in data:
        simpleDict = SimpleDict(data["Items"])
        if shopList >= 0:
            lpData = client.scan(
                TableName='ListProduct'
                )
            for slItem in simpleDict:
                thisSl = next((l for l in lpData["Items"] if l["product"].get("N") == str(slItem["id"]) and l["shopList"].get("N") == str(shopList)), "")
                if thisSl == "":
                    slItem["thisSl"] = False
                    slItem["listProductId"] = -1
                    slItem["note"] = ""
                else:
                    slItem["thisSl"] = thisSl["active"].get("BOOL")
                    slItem["listProductId"] = thisSl["id"].get("N")
                    slItem["amount"] = thisSl["amount"].get("N")
                    slItem["portion"] = thisSl["portion"].get("N")
                    #print("slItem")
                    #print(slItem)
                    #print("thisSl")
                    #print(thisSl)
                    slItem["note"] = thisSl["note"].get("S")
        if meal >= 0:
            mData = client.scan(
                TableName='MealProduct'
                )
            for mItem in simpleDict:
                thisMl = next((m for m in mData["Items"] if m["product"].get("N") == str(mItem["id"]) and m["meal"].get("N") == str(meal)), "")
                if thisMl == "":
                    mItem["thisMl"] = False
                    mItem["mealProductId"] = -1
                else:
                    mItem["thisMl"] = thisMl["active"].get("BOOL")
                    mItem["mealProductId"] = thisMl["id"].get("N")
                    mItem["amount"] = thisMl["amount"].get("N")
                    mItem["portion"] = thisMl["portion"].get("N")
        if sortfield == "areaOrder":
            simpleDict.sort(key=lambda e: (int(e["areaOrder"]) * 1000) + int(e["order"]) , reverse=sortorder=="desc")
        else:
            simpleDict.sort(key=lambda e: e[sortfield], reverse=sortorder=="desc")
        responseText = json.dumps(simpleDict)
    else:
        responseText = "No records found"
    
    response = {
        'statusCode': 200,
        'body': responseText,
        'headers': {
            'Content-Type': 'application/json',
            'Access-Control-Allow-Origin': '*'
        },
    }
  
    return response