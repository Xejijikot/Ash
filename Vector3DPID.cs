using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace IngameScript
{
    public class Vector3D_PID
    {
        public double Kp { get; set; } = 0;
        public double Ki { get; set; } = 0;
        public double Kd { get; set; } = 0;
        public Vector3D Value { get; private set; }

        double _timeStep = 0;
        double _inverseTimeStep = 0;
        Vector3D _errorSum = new Vector3D(0, 0, 0);
        Vector3D _lastError = new Vector3D(0, 0, 0);
        bool _firstRun = true;
        public Vector3D_PID(double kp, double ki, double kd, double timeStep)
        {
            Kp = kp;
            Ki = ki;
            Kd = kd;
            _timeStep = timeStep;
            _inverseTimeStep = 1 / _timeStep;
        }

        protected virtual Vector3D GetIntegral(Vector3D currentError, Vector3D errorSum, double timeStep)
        {
            return errorSum + currentError * timeStep;
        }

        public Vector3D Control(Vector3D error)
        {
            //Compute derivative term
            Vector3D errorDerivative = (error - _lastError) * _inverseTimeStep;

            if (_firstRun)
            {
                errorDerivative = new Vector3D(0, 0, 0);
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

        public Vector3D Control(Vector3D error, double timeStep)
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
            _errorSum = new Vector3D(0, 0, 0);
            _lastError = new Vector3D(0, 0, 0);
            _firstRun = true;
        }
    }
}
