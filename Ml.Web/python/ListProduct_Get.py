import json
import boto3

def SimpleDict(origDict):
    client = boto3.client('dynamodb')

    portions = client.scan(
        TableName='Portion'
    )

    products = client.scan(
        TableName='Product'
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
                    elif prop=="product":
                        propItem = next((p for p in products["Items"] if p["id"].get("N") == str(propVal)), "")
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
        paramField = item["param"].split("_")
        sortfield = paramField[0]
        sortorder = paramField[1]
        shopListId = paramField[2]
    except Exception as error:
        sortfield = "id"
        sortorder = "asc"
        shopListId = ""

    data = client.scan(
        TableName='ListProduct'
        )
    
    if "Items" in data:
        simpleDict = SimpleDict(data["Items"])
        if shopListId != "" and shopListId != 0:
            simpleDict = list(filter(lambda i: i["shopList"] == int(shopListId), simpleDict))
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