
using System;

namespace BioMap
{
  /// <summary>
  /// Optimierung mit der Methode der kleinsten Quadratsummen.
  /// </summary>
  public class LeastSquareFit
  {
    public delegate double CalcSquareSumDelegate(double[] daParams,double[][] daaPoints);
    public enum Method
    {
      None = 0,
      Random = 1,
      Monotone = 2,
      RandomThenMonotone = 3,
      Directed = 4,
    }
    /// <summary>
    /// Optimierung durchführen. Eine parametrierte Kurve wird einer Menge 
    /// von übergebenen Punkten angenähert.
    /// </summary>
    /// <param name="daStartParams">
    /// Startparameter. Die Feldgröße muss der Anzahl der von der Methode 
    /// calcSquareSum() erwarteten Parameter entsprechen.
    /// </param>
    /// <param name="daParamRanges">
    /// Parameter-Wertebereiche. Die Feldgröße muss den Startparametern entsprechen. 
    /// Jedes Element besteht aus einem Feld mit 2 Elementen, der Unter- 
    /// und der Obergrenze des Wertebereichs.
    /// </param>
    /// <param name="daaPoints">
    /// Die Punkte, an die die parametrierte Kurve angenähert werden soll.
    /// </param>
    /// <param name="calcSquareSum">
    /// Die Methode, die die Quadratsumme der Abweichungen der übergebenen 
    /// Punkte von der parametrierten Kurve zurückliefert.
    /// </param>
    /// <param name="dStartStepSize">
    /// Die anfängliche Schrittweite für die Variation der Parameter.
    /// </param>
    /// <param name="dEpsilon">
    /// Die Genauigkeit, mit der die Parameter ermittelt werden sollen.
    /// </param>
    /// <param name="daBestParams">
    /// Die optimierten Parameter.
    /// </param>
    public void Optimize(
      double[][] daaParamRanges,
      double[][] daaPoints,
      CalcSquareSumDelegate calcSquareSum,
      double dStartStepSize,
      double dEpsilon,
      out double[] daBestParams,
      Method enMethod = Method.Random) {
      //
      int nParamCount = daaParamRanges.Length;
      double[] daCurrentParams = new double[nParamCount];
      for (int iParam = 0;iParam<nParamCount;iParam++) {
        daCurrentParams[iParam]=0.5*(daaParamRanges[iParam][1]+daaParamRanges[iParam][0]);
      }
      if (enMethod==Method.Directed) {
        if (nParamCount>=2) {
          throw new ArgumentException("Only one parameter allowed with Directed method.");
        }
        double[] daParams = new double[nParamCount];
        double dSquareSum;
        double dCurrentStepSize = dStartStepSize;
        daBestParams=(double[])daCurrentParams.Clone();
        double dBestSquareSum = double.MaxValue;
        var daParamInterval = 
          (dCurrentStepSize<0)
          ?
          new[] { daaParamRanges[0][1],daaParamRanges[0][0] }
          :
          new[] { daaParamRanges[0][0],daaParamRanges[0][1] }
          ;
        while (Math.Abs(dCurrentStepSize)>dEpsilon) {
          int nSteps = (int)Math.Ceiling((daParamInterval[1]-daParamInterval[0])/dCurrentStepSize);
          for (int iStep = 0;iStep<nSteps;iStep++) {
            daParams[0]=daParamInterval[0]+iStep*dCurrentStepSize;
            dSquareSum=calcSquareSum(daParams,daaPoints);
            if (dSquareSum<dBestSquareSum) {
              dBestSquareSum=dSquareSum;
              daBestParams=(double[])daParams.Clone();
            }
          }
          daParamInterval=new[] { daBestParams[0]-2*dCurrentStepSize,daBestParams[0]+2*dCurrentStepSize };
          dCurrentStepSize*=0.2;
        }
      } else if (enMethod==Method.Random) {
        Random random = new Random();
        double[] daParams = new double[nParamCount];
        double dSquareSum;
        double dCurrentStepSize = dStartStepSize;
        daBestParams=(double[])daCurrentParams.Clone();
        double dBestSquareSum = double.MaxValue;
        while (dCurrentStepSize>dEpsilon) {
          for (int nIteration = 0;nIteration<216;nIteration++) {
            for (int iParam = 0;iParam<nParamCount;iParam++) {
              daParams[iParam]=daCurrentParams[iParam]+dCurrentStepSize*(daaParamRanges[iParam][1]-daaParamRanges[iParam][0])*(random.NextDouble()-0.5);
            }
            dSquareSum=calcSquareSum(daParams,daaPoints);
            if (dSquareSum<dBestSquareSum) {
              dBestSquareSum=dSquareSum;
              daBestParams=(double[])daParams.Clone();
            }
          }
          daCurrentParams=(double[])daBestParams.Clone();
          dCurrentStepSize*=0.573;
        }
      } else if (enMethod==Method.Monotone) {
        if (nParamCount>=2) {
          throw new ArgumentException("Only one parameter allowed with Monotone method.");
        }
        double[] daParams = new double[nParamCount];
        double dSquareSum;
        double dCurrentStepSize = dStartStepSize;
        daBestParams=(double[])daCurrentParams.Clone();
        double dBestSquareSum = double.MaxValue;
        while (Math.Abs(dCurrentStepSize)>dEpsilon) {
          daParams[0]=daCurrentParams[0]+dCurrentStepSize;
          if (daParams[0]<daaParamRanges[0][0] || daParams[0]>daaParamRanges[0][1]) {
            break;
          }
          dSquareSum=calcSquareSum(daParams,daaPoints);
          if (dSquareSum<=dBestSquareSum) {
            dBestSquareSum=dSquareSum;
            daBestParams=(double[])daParams.Clone();
            daCurrentParams=(double[])daBestParams.Clone();
          } else {
            dCurrentStepSize*=-0.573;
          }
        }
      } else if (enMethod==Method.RandomThenMonotone) {
        if (nParamCount>=2) {
          throw new ArgumentException("Only one parameter allowed with Monotone method.");
        }
        Random random = new Random();
        double[] daParams = new double[nParamCount];
        double dSquareSum;
        double dCurrentStepSize = dStartStepSize;
        daBestParams=(double[])daCurrentParams.Clone();
        double dBestSquareSum = double.MaxValue;
        for (int nRandomLoop = 0;nRandomLoop<1;nRandomLoop++) {
          for (int nIteration = 0;nIteration<216;nIteration++) {
            for (int iParam = 0;iParam<nParamCount;iParam++) {
              daParams[iParam]=daaParamRanges[iParam][0]+(daaParamRanges[iParam][1]-daaParamRanges[iParam][0])*(random.NextDouble());
            }
            dSquareSum=calcSquareSum(daParams,daaPoints);
            if (dSquareSum<dBestSquareSum) {
              dBestSquareSum=dSquareSum;
              daBestParams=(double[])daParams.Clone();
            }
          }
          daCurrentParams=(double[])daBestParams.Clone();
          dCurrentStepSize*=0.573;
        }
        while (Math.Abs(dCurrentStepSize)>dEpsilon) {
          daParams[0]=daCurrentParams[0]+dCurrentStepSize;
          if (daParams[0]<daaParamRanges[0][0] || daParams[0]>daaParamRanges[0][1]) {
            dCurrentStepSize*=-0.573;
            continue;
          }
          dSquareSum=calcSquareSum(daParams,daaPoints);
          if (dSquareSum<=dBestSquareSum) {
            dBestSquareSum=dSquareSum;
            daBestParams=(double[])daParams.Clone();
            daCurrentParams=(double[])daBestParams.Clone();
          } else {
            dCurrentStepSize*=-0.573;
          }
        }
      } else {
        daBestParams=(double[])daCurrentParams.Clone();
      }
    }
  }
}
