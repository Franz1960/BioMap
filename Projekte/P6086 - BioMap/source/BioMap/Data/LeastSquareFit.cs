
//#define GRAPHICS

using System;

namespace BioMap
{
  /// <summary>
  /// Optimierung mit der Methode der kleinsten Quadratsummen.
  /// </summary>
  public class LeastSquareFit
  {
    public delegate double CalcSquareSumDelegate(double[] daParams,double[][] daaPoints);

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
      out double[] daBestParams) {
      //
#if GRAPHICS
      FormZedGraph formZedGraph=new FormZedGraph();
      formZedGraph.Hidden=false;
      //
      ZedGraph.PointPairList pplPoints=new ZedGraph.PointPairList();
      for (int i=0;i<daaPoints.Length;i++) {
        pplPoints.Add(daaPoints[i][1],daaPoints[i][0]);
      }
      ZedGraph.LineItem lineItemPoints=formZedGraph.ZedGraphControl.GraphPane.AddCurve("",pplPoints,Color.Blue);
      lineItemPoints.Symbol.Type=ZedGraph.SymbolType.Diamond;
      lineItemPoints.Line.IsVisible=false;
#endif
      Random random=new Random();
      int nParamCount=daaParamRanges.Length;
      double[] daCurrentParams=new double[nParamCount];
      for (int iParam=0;iParam<nParamCount;iParam++) {
        daCurrentParams[iParam]=0.5*(daaParamRanges[iParam][1]+daaParamRanges[iParam][0]);
      }
      double[] daParams=new double[nParamCount];
      double dSquareSum;
      double dCurrentStepSize=dStartStepSize;
      daBestParams=(double[])daCurrentParams.Clone();
      double dBestSquareSum=double.MaxValue;
      while (dCurrentStepSize>dEpsilon) {
        for (int nIteration=0;nIteration<216;nIteration++) {
          for (int iParam=0;iParam<nParamCount;iParam++) {
            daParams[iParam]=daCurrentParams[iParam]+dCurrentStepSize*(daaParamRanges[iParam][1]-daaParamRanges[iParam][0])*(random.NextDouble()-0.5);
          }
          dSquareSum=calcSquareSum(daParams,daaPoints);
          if (dSquareSum<dBestSquareSum) {
            dBestSquareSum=dSquareSum;
            daBestParams=(double[])daParams.Clone();
          }
        }
#if GRAPHICS
        float fRadius=(float)daCurrentParams[0];
        PointF ptCenter=new PointF((float)daCurrentParams[1],(float)daCurrentParams[2]);
        ZedGraph.PointPairList pplLine=new ZedGraph.PointPairList();
        for (int i=0;i<daaPoints.Length;i++) {
          double dPhi=daaPoints[i][1];
          pplLine.Add(dPhi,fRadius+ptCenter.X*Math.Cos(dPhi)+ptCenter.Y*Math.Sin(dPhi));
        }
        ZedGraph.LineItem lineItemLine=formZedGraph.ZedGraphControl.GraphPane.AddCurve("",pplLine,Color.Blue);
        lineItemLine.Symbol.IsVisible=false;
        lineItemLine.Line.IsVisible=true;
        formZedGraph.ZedGraphControl.AxisChange();
        formZedGraph.ZedGraphControl.AxisChange();
        formZedGraph.ZedGraphControl.AxisChange();
        formZedGraph.ZedGraphControl.Invalidate();
#endif
        daCurrentParams=(double[])daBestParams.Clone();
        dCurrentStepSize*=0.573;
      }
    }

    /// <summary>
    /// Funktion, die die Quadratsumme der Abweichungen einer Menge von übergebenen 
    /// Punkten von einem zweidimensionalen Kreis zurückliefert. Diese Methode 
    /// erfüllt die Signatur eines <see cref="CalcSquareSumDelegate"/> und kann 
    /// deshalb an die Methode <see cref="Optimize"/> übergeben werden.
    /// </summary>
    /// <param name="daParams">
    /// Die Parameter: [0]=Radius, [1]=Mittelpunkt X, [2]=Mittelpunkt Y.
    /// </param>
    /// <param name="daaPoints">
    /// Die Punkte. Jeder Punkt wird durch ein Feld mit den zwei Elementen X und 
    /// Y bestimmt.
    /// </param>
    /// <returns>
    /// Die Quadratsumme der Abweichungen.
    /// </returns>
    public double CalcSquareSumForCircle(double[] daParams,double[][] daaPoints) {
      double dRadius=daParams[0];
      double dCenterX=daParams[1];
      double dCenterY=daParams[2];
      double dSquareSum=0;
      double dDeltaX;
      double dDeltaY;
      double dDeviation;
      for (int i=0;i<daaPoints.Length;i++) {
        dDeltaX=daaPoints[i][0]-dCenterX;
        dDeltaY=daaPoints[i][1]-dCenterY;
        dDeviation=Math.Sqrt(dDeltaX*dDeltaX+dDeltaY*dDeltaY)-dRadius;
        dSquareSum+=dDeviation*dDeviation;
      }
      return dSquareSum;
    }

    /// <summary>
    /// Funktion, die die Quadratsumme der Abweichungen einer Menge von übergebenen 
    /// Punkten von einer zweidimensionalen Kurve zurückliefert, die entsteht, wenn 
    /// eine Zylinderoberfläche im einem Lineartaster mit flacher Spitze abgetastet 
    /// wird. Diese Methode 
    /// erfüllt die Signatur eines <see cref="CalcSquareSumDelegate"/> und kann 
    /// deshalb an die Methode <see cref="Optimize"/> übergeben werden.
    /// </summary>
    /// <param name="daParams">
    /// Die Parameter: [0]=Radius, [1]=Mittelpunkt X, [2]=Mittelpunkt Y.
    /// </param>
    /// <param name="daaPoints">
    /// Die Punkte in Polarkoordinaten. Jeder Punkt wird durch ein Feld mit den 
    /// zwei Elementen r und phi bestimmt.
    /// </param>
    /// <returns>
    /// Die Quadratsumme der Abweichungen.
    /// </returns>
    public double CalcSquareSumForPseudoCircleInPolarCoord(double[] daParams,double[][] daaPoints) {
      double dRadius=daParams[0];
      double dCenterX=daParams[1];
      double dCenterY=daParams[2];
      double dSquareSum=0;
      double dPhi;
      double dRadiusExpected;
      double dDeviation;
      for (int i=0;i<daaPoints.Length;i++) {
        dPhi=daaPoints[i][1];
        dRadiusExpected=dRadius+dCenterX*Math.Cos(dPhi)+dCenterY*Math.Sin(dPhi);
        dDeviation=daaPoints[i][0]-dRadiusExpected;
        dSquareSum+=dDeviation*dDeviation;
      }
      return dSquareSum;
    }

  }
}
