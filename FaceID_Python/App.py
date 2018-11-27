import sys
from flask import Flask, jsonify
from flask_cors import CORS
from flask_restful import Resource, Api, reqparse

sys.path.append('./Model')
sys.path.append('./Processing')

from Model.FaceID import FaceID

app = Flask(__name__)
CORS(app)
api = Api(app)
faceId = FaceID()

parser = reqparse.RequestParser()
parser.add_argument('subject')

@app.route("/")
def hello():
    return "<h1 style='color:blue'>ALFI FaceID</h1>"

class CheckStatus(Resource):
    def get(self):
        if faceId:
            return {'status': 'OK'}, 200
        else:
            return {'status': 'Not_Working'}, 500

class Identify(Resource):
    def get(self):
        args = parser.parse_args()
        if not args['subject']:
            return {'error': 'No subject to identify'}, 400
        sbj = args['subject']
        # img = Image.open(io.BytesIO(img))
        # text, boxes = od.detect(img, mode)
        return {'match': True, 'identity': 'sbj-xx'}

api.add_resource(CheckStatus, '/status')
api.add_resource(Identify, '/id')

if __name__ == '__main__':
    app.run(debug=False)

    faceID.load('best')
