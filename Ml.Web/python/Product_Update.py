import json
import boto3
from decimal import Decimal

def lambda_handler(event, context):
    #client = boto3.client('dynamodb')
    table = boto3.resource('dynamodb').Table('Product')
    
    try:
        items = json.loads(event["body"])
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
    
    for item in items:
        thisId = int(item["id"])
        thisAmount = Decimal(item["amount"])
        try:
            table.update_item(
                Key={'id': thisId},
                UpdateExpression="set amount = :r",
                ExpressionAttributeValues={
                    ':r': thisAmount
                }
            )    
        except Exception as error:
            print(error)
            response = {
            'statusCode': 401,
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