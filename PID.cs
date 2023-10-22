using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IngameScript
{
    public class PID
    {
        public double Kp { get; set; } = 0;
        public double Ki { get; set; } = 0;
        public double Kd { get; set; } = 0;
        public double Value { get; private set; }

        double _timeStep = 0;
        double _inverseTimeStep = 0;
        double _errorSum = 0;
        double _lastError = 0;
        bool _firstRun = true;

        public PID(double kp, double ki, double kd, double timeStep)
        {
            Kp = kp;
            Ki = ki;
            Kd = kd;
            _timeStep = timeStep;
            _inverseTimeStep = 1 / _timeStep;
        }

        protected virtual double GetIntegral(double currentError, double errorSum, double timeStep)
        {
            return errorSum + currentError * timeStep;
        }

        public double Control(double error)
        {
            //Compute derivative term
            double errorDerivative = (error - _lastError) * _inverseTimeStep;

            if (_firstRun)
            {
                errorDerivative = 0;
                _firstRun = false;
            }

            //Get error sum
            _errorSum = GetIntegral(error, _errorSum, _timeStep);

            //Store this error as last error
            _lastError = error;
            //Construct output
            Value = Kp * error + Ki * _errorSum + Kd * errorDerivative;
            return Value;
        }

        public double Control(double error, double timeStep)
        {
            if (timeStep != _timeStep)
            {
                _timeStep = timeStep;
                _inverseTimeStep = 1 / _timeStep;
            }
            return Control(error);
        }

        public void Reset()
        {
            _errorSum = 0;
            _lastError = 0;
            _firstRun = true;
        }
    }
}
