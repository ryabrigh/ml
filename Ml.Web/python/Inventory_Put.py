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
        TableName='Product'
        )    

    try:
        for item in items:
            existingItem = next(i for i in existingData["Items"] if i["id"].get("N") == item["id"])
            #print(existingItem)
            existingItem["amount"]["N"] = item["amount"]
            #print(existingItem)
            client.put_item(
                TableName='Product',
                Item=existingItem
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