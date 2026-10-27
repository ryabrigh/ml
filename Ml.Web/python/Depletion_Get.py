import json
import boto3
from datetime import datetime

def SimpleDict(origDict):
    client = boto3.client('dynamodb')

    products = client.scan(
        TableName='Product'
    )

    portions = client.scan(
        TableName='Portion'
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
                    if prop=="product":
                        propItem = next((p for p in products["Items"] if p["id"].get("N") == str(propVal)), "")
                        propName = propItem["name"].get("S")
                        portionId = propItem["portion"].get("N")
                        itemPortion = next((t for t in portions["Items"] if t["id"].get("N") == str(portionId)), "")
                        portionName = itemPortion["name"].get("S")
                        newItem["portionName"] = portionName
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

    data = client.scan(
        TableName='Depletion'
        )    

    if "Items" in data:
        simpleDict = SimpleDict(data["Items"])
        simpleDict.sort(key=lambda e: e["product"])
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