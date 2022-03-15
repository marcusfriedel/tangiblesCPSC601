#include <Wire.h>
#include <Adafruit_MotorShield.h>
#include "utility/Adafruit_MS_PWMServoDriver.h"

const int LEFT = 1;
const int RIGHT = 2;
const int BOTH = 3;

const int dataArray[] = {1531, 4561, 6645, 6841, 2313, 7811, 2054, 6035, 3000, 7614}; //10 values
const int numDatapoints = 10;
int datapointIndex = 0;

const byte interruptPinA = 2;
const byte interruptPinB = 3;
const byte encoderPinA = 4;
const byte encoderPinB = 13;
const byte buttonPinA = 5;
const byte buttonPinB = 6;
const byte buttonPinBoth = 7;
const byte potentiometerPin = A0;

Adafruit_MotorShield AFMS = Adafruit_MotorShield(); 
Adafruit_DCMotor *motorA = AFMS.getMotor(1);
Adafruit_DCMotor *motorB = AFMS.getMotor(2);

double motorPosA = 0;
double motorPosB = 0;
double targetPos = 0;

int potValue = 0;
bool motorForward = true;
double motorSpeed = 0;

double Kp = 0.3;
double controlSignal = 0;

unsigned long nextMoveTime = 0;
bool stateMoving = false;
const int epsilon = 50;



void setup() {
  // set up potentiometer pin. The potentiometer sets the motor speed.
  pinMode(potentiometerPin, INPUT);

  //set up the encoder pins and associated interrupts
  pinMode(interruptPinA, INPUT_PULLUP);
  pinMode(interruptPinB, INPUT_PULLUP);
  attachInterrupt(digitalPinToInterrupt(interruptPinA), EncoderChangeA, RISING);
  attachInterrupt(digitalPinToInterrupt(interruptPinB), EncoderChangeB, RISING);


  // set up the motor shield
  AFMS.begin();
  Serial.begin(9600);
}



void loop() {
  //move to the next target value
  //once you reach the position, wait 10 seconds
  //if the user interrupts the process by pressing a button:
    //do the buttonpress
    //start the 10 second timer
    //stop trying to reach the goal

  Serial.print(
    (String)millis() + '\t' + 
    (stateMoving ? "seeking" : "       ") + '\t' +
    (String)(targetPos) + '\t' + 
    "nextMoveTime: " + (String)nextMoveTime + '\t'
    );

  if(stateMoving){
    CalcPControllerSpeed();
    Serial.print("PSpeed: " + (String)motorSpeed + '\t');
    SetMotorSpeeds(BOTH);

    //if the device has reached the desired position, stop the movement
    if(abs(targetPos - motorPosA) < epsilon)
      StopSeeking();
  }else if(millis() > nextMoveTime){
    targetPos = GetNextTargetPos();
    stateMoving = true;
  }

  //get the user speed from the potentiometer
  potValue = analogRead(potentiometerPin);
  MapSpeed(map(potValue, 0, 667, -90, 90));
  
  Serial.println((String)potValue + '\t' +
                 (String)motorSpeed + '\t' +
                 (motorForward ? "FORWARD" : "BACKWARD") + "  " + '\t' +
                 (String)motorPosA + '\t' +
                 (String)motorPosB);

  // run motor A if buttonA or buttonBoth is pressed
  if(digitalRead(buttonPinA) == HIGH 
  || digitalRead(buttonPinBoth) == HIGH){
    SetMotorSpeeds(LEFT);
    StopSeeking();    
  }
  else if(!stateMoving) motorA->setSpeed(0);
  
  // run motor B if buttonB or buttonBoth is pressed
  if(digitalRead(buttonPinB) == HIGH 
  || digitalRead(buttonPinBoth) == HIGH){
    SetMotorSpeeds(RIGHT);
    StopSeeking();    
  }
  else if(!stateMoving) motorB->setSpeed(0);
}






void CalcPControllerSpeed(){
  controlSignal = Kp * (targetPos - motorPosA);
  if(abs(controlSignal) > 90)
    controlSignal = 90 * (controlSignal > 0 ? 1 : -1);
  MapSpeed((int)controlSignal);
}

void StopSeeking(){
  motorPosB = motorPosA;
  stateMoving = false;
  nextMoveTime = millis() + 10000;
}

void MapSpeed(int rawSpeed){
  //rawSpeed is bounded between -90 to 90
  motorForward = (rawSpeed > 0);
  motorSpeed = abs(rawSpeed) + 150;
  //valid motor speeds are 150-240
}

void SetMotorSpeeds(int whichMotor){
  //whichMotor: 1-left 2-right 3-both

  if(whichMotor == LEFT || whichMotor == BOTH){
    motorA->run((motorForward ? FORWARD : BACKWARD));
    motorA->setSpeed(motorSpeed); 
  }
  
  if(whichMotor == RIGHT || whichMotor == BOTH){
    motorB->run((motorForward ? FORWARD : BACKWARD));
    motorB->setSpeed(motorSpeed);
  }
}

int GetNextTargetPos(){
  datapointIndex = (datapointIndex + 1) % numDatapoints;
  return dataArray[datapointIndex];
}

void EncoderChangeA(){
  if(digitalRead(encoderPinA) == HIGH)
    motorPosA++;
  else
    motorPosA--;
}

void EncoderChangeB(){
  if(digitalRead(encoderPinB) == HIGH)
    motorPosB++;
  else
    motorPosB--;
}
