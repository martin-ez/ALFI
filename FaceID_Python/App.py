import sys, os
from flask import Flask, jsonify
from flask_cors import CORS
from flask_restful import Resource, Api, reqparse
import numpy as np

sys.path.append('./Model')
sys.path.append('./Processing')

from Model.FaceID import FaceID
from Model.SampleGenerator import getRGBD
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
        preprocess(os.path.join(faceID.to_identify_path, 'sbj-'+str(sbj)), os.path.join(faceID.to_identify_processed_path, 'sbj-'+str(sbj)), 'DC', 0)
        subject = getRGBD(sbj, 0, faceID.to_identify_processed_path)
        current = 0
        while (os.path.isdir(os.path.join(faceID.dataset_path, 'sbj-'+str(current)))):
            next = False
            capture = 0
            while not next:
                other = getRGBD(current, capture, faceID.dataset_path)
                if other is None:
                    next = True
                else:
                    sample = np.array([subject, other])
                    if faceID.predict(sample, threshold=0.2):
                        return {'Match': True, 'Identity': current}
                capture = capture + 1
            current = current + 1
        return {'Match': False, 'Identity': 0}

class AddToRegistry(Resource):
    def get(self):
        args = parser.parse_args()
        if not args['subject']:
            return {'error': 'No subject to identify'}, 400
        sbj = args['subject']
        in_path = os.path.join(faceID.to_process_path, 'sbj-'+str(sbj))
        out_path = os.path.join(faceID.dataset_path, 'sbj-'+str(faceID.current_sbj))
        i = 0
        while (i < 9):
            preprocess(in_path, out_path, 'DC', i)
            i = i+1
        faceID.current_sbj = faceID.current_sbj + 1
        return {'added': True}

class Train(Resource):
    def get(self):
        faceID.train(60, 'training')
        return {'finished': True}

api.add_resource(CheckStatus, '/status')
api.add_resource(Identify, '/id')
api.add_resource(AddToRegistry, '/process')
api.add_resource(Train, '/train')

if __name__ == '__main__':
    faceID = FaceID()
    faceID.load('training')
    app.run(debug=False)
