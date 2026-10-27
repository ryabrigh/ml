import json
import boto3

def lambda_handler(event, context):
    client = boto3.client('dynamodb')

    try:
        item = json.loads(event["body"])
    except Exception as error:
        response = {
        'statusCode': 500,
        'body': json.dumps(event),
        'headers': {
            'Content-Type': 'application/json',
            'Access-Control-Allow-Origin': '*'
            }
        }
        return response
    
    if "id" in item:
        print("got an id")
    else:
        #no id so get one
        existingData = client.scan(
            TableName='Meal'
            )    

        if "Items" in existingData:
            existingIds = []
            for existingItem in existingData["Items"]:
                for value in existingItem["id"].values():
                    existingIds.append(int(value))
            newId = str(max(existingIds) + 1)
        else:
            print("No records found")
            newId = "1"
        item["id"] = {'N':newId}

    # does item body have an active value?
    if not "active" in item:
        print("no active given")
        item["active"]={'BOOL': True}
    
    try:
        data = client.put_item(
            TableName='Meal',
            Item=item
        )
    except Exception as error:
        print(error)
        response = {
        'statusCode': 400,
        'body': json.dumps(event),
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