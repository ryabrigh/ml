import json
import boto3
from datetime import datetime

def DateFields():
    return {"listDate"}
    
def SimpleDict(origDict):
    newDict = []
    for item in origDict:
        newItem = {}
        for prop in item:
            propVal = item[prop].get("N")
            if propVal == None:
                propVal = item[prop].get("S")
            else:
                propVal = int(propVal)
            if propVal == None:
                propVal = item[prop].get("BOOL")
            newItem[prop] = propVal
        newDict.append(newItem)
    return newDict
    
def lambda_handler(event, context):
    client = boto3.client('dynamodb')

    data = client.scan(
        TableName='ShopList'
        )    

    if "Items" in data:
        simpleDict = SimpleDict(data["Items"])
        simpleDict.sort(key=lambda e: datetime.strptime(e['listDate'], "%d/%m/%Y"), reverse=True)
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