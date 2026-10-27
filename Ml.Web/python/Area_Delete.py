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

    try:
        data = client.delete_item(
            TableName='Area',
            Key={"id": {"N": str(id)}}
        )
        responseText = json.dumps("")
        statusCode = 200
    except Exception as error:
        responseText = json.dumps(error)
        statusCode = 400

    response = {
        'statusCode': statusCode,
        'body': responseText,
        'headers': {
            'Content-Type': 'application/json',
            'Access-Control-Allow-Origin': '*'
        },
    }
  
    return response