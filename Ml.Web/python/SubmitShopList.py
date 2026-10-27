import json
import boto3

def lambda_handler(event, context):
    client = boto3.client('dynamodb')

    try:
        item = json.loads(event["body"])
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
    
    #response = {
    #'statusCode': 200,
    #'body': json.dumps(event),
    #'headers': {
    #    'Content-Type': 'application/json',
    #    'Access-Control-Allow-Origin': '*'
    #    }
    #}
    #return response
    
    #TODO:
    #get a scan of the ListProducts table
    #and create a json list of existing records where shopList is id
    #for that list, set active=false where product isn't in selected list in body
    #add to that list if selected isn't in there with active=true
    #what are we going to set as order? might need to send more complex data here from page e.g. amount, portion, order
    
    try:
        data = client.put_item(
            TableName='ListProduct',
            Item=item
        )
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
        

    response = {
        'statusCode': 200,
        'body': json.dumps('success'),
        'headers': {
            'Content-Type': 'application/json',
            'Access-Control-Allow-Origin': '*'
        },
    }
  
    return response