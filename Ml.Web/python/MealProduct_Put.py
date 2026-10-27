import json
import boto3

def lambda_handler(event, context):
    client = boto3.client('dynamodb')

    try:
        items = json.loads(event["body"])
    except Exception as error:
        response = {
        'statusCode': 400,
        'body': json.dumps(event["body"]),
        'headers': {
            'Content-Type': 'application/json',
            'Access-Control-Allow-Origin': '*'
            }
        }
        return response

    existingData = client.scan(
        TableName='MealProduct'
        )    

    if "Items" in existingData:
        existingIds = []
        for existingItem in existingData["Items"]:
            for value in existingItem["id"].values():
                existingIds.append(int(value))
        newIdInt = max(existingIds)
    else:
        print("No records found")
        newIdInt = 0
    
    for item in items:
        print(item)
        if "id" in item:
            print("got an id")
        else:
            #no id so get one
            newIdInt = newIdInt + 1
            newId = str(newIdInt)
            item["id"] = {'N':newId}
            
        # does item body have an active value?
        if not "active" in item:
            print("no active given")
            item["active"]={'BOOL': True}
    
    try:
        for item in items:
            client.put_item(
                TableName='MealProduct',
                Item=item
            )
    except Exception as error:
        print(error)
        response = {
        'statusCode': 400,
        'body': json.dumps(event["body"]),
        'headers': {
            'Content-Type': 'application/json',
            'Access-Control-Allow-Origin': '*'
            }
        }
        return response
        
    response = {
        'statusCode': 200,
        'body': json.dumps('success'),
        'headers': {
            'Content-Type': 'application/json',
            'Access-Control-Allow-Origin': '*'
        },
    }
  
    return response