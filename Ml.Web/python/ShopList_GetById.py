import json
import boto3

def lambda_handler(event, context):
    client = boto3.client('dynamodb')

    try:
        item = event["pathParameters"]
    except Exception as error:
        response = {
        'statusCode': 400,
        'body': json.dumps(event),
        'headers': {
            'Content-Type': 'application/json',
            'Access-Control-Allow-Origin': '*'
            }
        }
        return response
    
    id = item["id"]

    data = client.get_item(
        TableName='ShopList',
        Key={"id": {"N": str(id)}}
    )

    if "Item" in data:
        responseText = json.dumps(data["Item"])
    else:
        responseText = "No record found for id " + id
    
    response = {
        'statusCode': 200,
        #'body': json.dumps(data["Item"]),
        'body': responseText,
        'headers': {
            'Content-Type': 'application/json',
            'Access-Control-Allow-Origin': '*'
        },
    }
  
    return response