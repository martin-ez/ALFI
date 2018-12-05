import sys, os
from flask import Flask, jsonify
from flask_cors import CORS
from flask_restful import Resource, Api, reqparse
import numpy as np

sys.path.append('./Model')
sys.path.append('./Processing')

from Model.FaceID import FaceID
from Model.ImageReader import getGDII
from Processing.Preprocessing import preprocess

app = Flask(__name__)
CORS(app)
api = Api(app)
faceID = None

parser = reqparse.RequestParser()
parser.add_argument('subject')

@app.route("/")
def hello():
    return "<h1 style='color:blue'>ALFI FaceID</h1>"

class CheckStatus(Resource):
    def get(self):
        if faceID:
            return {'status': 'OK'}, 200
        else:
            return {'status': 'Not_Working'}, 500

class Identify(Resource):
    def get(self):
        args = parser.parse_args()
        if not args['subject']:
            return {'error': 'No subject to identify'}, 400
        sbj = args['subject']
        identify_path = os.path.join(os.path.abspath(os.sep), 'ALFI_Img_Repo','ToIdentify')
        processed_path = os.path.join(os.path.abspath(os.sep), 'ALFI_Img_Repo','ToIdentifyProcessed')
        registry_path = os.path.join(os.path.abspath(os.sep), 'ALFI_Img_Repo','Registry')
        preprocess(os.path.join(identify_path, 'sbj-'+str(sbj)), os.path.join(processed_path, 'sbj-'+str(sbj)), 'DC', 0)
        subject = getGDII(sbj, 0, processed_path)
        current = 0
        while (os.path.isdir(os.path.join(registry_path, 'sbj-'+str(current)))):
            next = False
            capture = 0
            while not next:
                other = getGDII(current, capture, registry_path)
                if other is None:
                    next = True
                else:
                    sample = np.array([subject, other])
                    if faceID.predict(sample, threshold=0.25):
                        return {'Match': True, 'Identity': current}
                capture = capture + 1
            current = current + 1
        return {'Match': False, 'Identity': matched}

class AddToRegistry(Resource):
    def get(self):
        pass

class Train(Resource):
    def get(self):
        pass

api.add_resource(CheckStatus, '/status')
api.add_resource(Identify, '/id')

if __name__ == '__main__':
    faceID = FaceID()
    faceID.load('last')
    app.run(debug=False)
