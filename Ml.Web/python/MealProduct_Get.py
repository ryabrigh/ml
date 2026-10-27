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

    meals = client.scan(
        TableName='Meal'
    )
    #print(meals)
    newDict = []
    for item in origDict:
        #print(item)
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
                    #print(propVal)
                    #maybe an id field?
                    if prop=="portion":
                        propItem = next((p for p in portions["Items"] if p["id"].get("N") == str(propVal)), "")
                        propName = propItem["name"].get("S")
                    elif prop=="product":
                        propItem = next((p for p in products["Items"] if p["id"].get("N") == str(propVal)), "")
                        propName = propItem["name"].get("S")
                    elif prop=="meal":
                        propItem = next((m for m in meals["Items"] if m["id"].get("N") == str(propVal)), "")
                        #print(propItem)
                        #print(propItem["name"])
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
        id = item["id"]
    except Exception as error:
        id = ""

    data = client.scan(
        TableName='MealProduct'
        )
    
    if "Items" in data:
        simpleDict = SimpleDict(data["Items"])
        if (id != ""):
            simpleDict = list(filter(lambda i: i["meal"] == int(id), simpleDict))
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