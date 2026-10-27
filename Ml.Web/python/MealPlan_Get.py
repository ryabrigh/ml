import json
import boto3

def SimpleDict(origDict):
    client = boto3.client('dynamodb')

    meals = client.scan(
        TableName='Meal'
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
                    if prop=="meal":
                        propItem = next((p for p in meals["Items"] if p["id"].get("N") == str(propVal)), "")
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
        mealPlanId = item["plan"]
        print(mealPlanId)
    except Exception as error:
        print("split fails")
        mealPlanId = ""

    data = client.scan(
        TableName='MealPlan'
        )
    
    if "Items" in data:
        simpleDict = SimpleDict(data["Items"])
        if mealPlanId != "" and mealPlanId != 0:
            #print(int(mealPlanId))
            simpleDict = list(filter(lambda i: i["plan"] == int(mealPlanId), simpleDict))
        simpleDict.sort(key=lambda e: (e["day"], e["section"]))
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