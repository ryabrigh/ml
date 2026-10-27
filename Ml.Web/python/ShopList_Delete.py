import json
import boto3

def lambda_handler(event, context):
    client = boto3.client('dynamodb')
    dynamodb = boto3.resource('dynamodb')
    table = dynamodb.Table('ListProduct') # Change to your table name

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

    listProductData = client.scan(
        TableName='ListProduct'
    )
    #print(listProductData["Items"])
    listProductIds = []
    listProductShopList = list(filter(lambda x: x["shopList"].get("N") == id, listProductData["Items"]))
    for items in listProductShopList:
        listProductIds.append(items["id"].get("N"))
    print(listProductIds)
    #return
    
    try:
        #with table.batch_writer() as batch:
        for thisItem in listProductIds:
            #print(thisItem)
            response = client.delete_item(
                TableName='ListProduct',
                Key={"id": {"N": str(thisItem)}})
            #print(response)
    except Exception as error:
        print(error)
        responseText = json.dumps(event)
        statusCode = 400

    try:
        data = client.delete_item(
            TableName='ShopList',
            Key={"id": {"N": str(id)}}
        )
        responseText = json.dumps("")
        statusCode = 200
    except Exception as error:
        responseText = json.dumps(event)
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