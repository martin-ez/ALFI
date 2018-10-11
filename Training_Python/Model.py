from tensorflow.keras.models import Sequential, Model
from tensorflow.keras.layers import Dense, Activation, Flatten, Dropout, Lambda, ELU, concatenate, GlobalAveragePooling2D, Input, BatchNormalization, SeparableConv2D, Subtract, concatenate
from tensorflow.keras.activations import relu, softmax
from tensorflow.keras.layers import Convolution2D, MaxPooling2D, AveragePooling2D
from tensorflow.keras.optimizers import Adam, RMSprop, SGD
from tensorflow.keras.regularizers import l2
from tensorflow.keras import backend as K
import numpy as np
from SampleGenerator import sample
import csv

def euclidean_distance(inputs):
    assert len(inputs) == 2, \
        'Euclidean distance needs 2 inputs, %d given' % len(inputs)
    u, v = inputs
    return K.sqrt(K.sum((K.square(u - v)), axis=1, keepdims=True))

def contrastive_loss(y_true,y_pred):
    margin=1.
    return K.mean((1. - y_true) * K.square(y_pred) + y_true * K.square(K.maximum(margin - y_pred, 0.)))

def fire(x, squeeze=16, expand=64):
    x = Convolution2D(squeeze, (1,1), padding='valid')(x)
    x = Activation('relu')(x)

    left = Convolution2D(expand, (1,1), padding='valid')(x)
    left = Activation('relu')(left)

    right = Convolution2D(expand, (3,3), padding='same')(x)
    right = Activation('relu')(right)

    x = concatenate([left, right], axis=3)
    return x

def squeezeNet():
    img_input=Input(shape=(200,200,4))

    x = Convolution2D(64, (5, 5), strides=(2, 2), padding='valid')(img_input)
    x = BatchNormalization()(x)
    x = Activation('relu')(x)
    x = MaxPooling2D(pool_size=(3, 3), strides=(2, 2))(x)
    x = fire(x, squeeze=16, expand=16)
    x = fire(x, squeeze=16, expand=16)
    x = MaxPooling2D(pool_size=(3, 3), strides=(2, 2))(x)
    x = fire(x, squeeze=32, expand=32)
    x = fire(x, squeeze=32, expand=32)
    x = MaxPooling2D(pool_size=(3, 3), strides=(2, 2))(x)
    x = fire(x, squeeze=48, expand=48)
    x = fire(x, squeeze=48, expand=48)
    x = fire(x, squeeze=64, expand=64)
    x = fire(x, squeeze=64, expand=64)
    x = Dropout(0.2)(x)
    x = Convolution2D(512, (1, 1), padding='same')(x)
    out = Activation('relu')(x)

    return Model(img_input, out)
    return modelsqueeze

def faceIDNet():
    im_in = Input(shape=(200,200,4))

    modelsqueeze = squeezeNet()
    x1 = modelsqueeze(im_in)
    x1 = Flatten()(x1)
    x1 = Dense(512, activation="relu")(x1)
    x1 = Dropout(0.2)(x1)
    feat_x = Dense(128, activation="linear")(x1)
    feat_x = Lambda(lambda  x: K.l2_normalize(x,axis=1))(feat_x)
    model_top = Model(inputs = [im_in], outputs = feat_x)

    im_in1 = Input(shape=(200,200,4))
    im_in2 = Input(shape=(200,200,4))

    feat_x1 = model_top(im_in1)
    feat_x2 = model_top(im_in2)

    lambda_merge = Lambda(euclidean_distance)([feat_x1, feat_x2])

    model_final = Model(inputs = [im_in1, im_in2], outputs = lambda_merge)

    adam = Adam(lr=0.001)

    sgd = SGD(lr=0.001, momentum=0.9)

    model_final.compile(optimizer=adam, loss=contrastive_loss)

    return model_final

def generator(batch_size):
    while 1:
        x=[]
        y=[]
        switch=True
        for _ in range(batch_size):
            if switch:
                x.append(sample(correct=True, val=False, d_channel='index'))
                y.append(np.array([0.]))
            else:
                x.append(sample(correct=False, val=False, d_channel='index'))
                y.append(np.array([1.]))
            switch=not switch
        x = np.asarray(x)
        y = np.asarray(y)
        yield [x[:,0],x[:,1]],y

def val_generator(batch_size):
    while 1:
        x=[]
        y=[]
        switch=True
        for _ in range(batch_size):
            if switch:
                x.append(sample(correct=True, val=True, d_channel='index'))
                y.append(np.array([0.]))
            else:
                x.append(sample(correct=False, val=True, d_channel='index'))
                y.append(np.array([1.]))
            switch=not switch
        x = np.asarray(x)
        y = np.asarray(y)
        yield [x[:,0],x[:,1]],y

class FaceID:

    def __init__(self):
        self.model = faceIDNet()
        self.outputs = None

    def train(self, epochs, log_steps):
        with open('train_log.csv', mode='w') as log:
            log = csv.writer(log, delimiter=',', quotechar='"', quoting=csv.QUOTE_MINIMAL)

            min_loss = 1.
            gen = generator(12)
            val_gen = val_generator(6)
            e = 0
            while e<epochs:
                self.model.fit_generator(gen, epochs=log_steps, steps_per_epoch=30)
                lossTrain = self.model.evaluate_generator(gen, steps=30)
                lossVal = self.model.evaluate_generator(val_gen, steps=30, verbose=1)
                print('* Epoch: '+str(e))
                print('* - Loss train: '+str(lossTrain))
                print('* - Loss val: '+str(lossVal))
                log.writerow([e, lossTrain, lossVal])
                if (lossVal < min_loss):
                    min_loss = lossVal
                    print('* + Best result so far. Saving model.')
                    self.model.save('faceIDModel_save_index.h5')
                e=e+log_steps

    def predict(self, inputs):
        out = self.model.predict(inputs)


if __name__ == "__main__":
    faceID = FaceID()
    faceID.model.summary()
    faceID.train(30, 1)
