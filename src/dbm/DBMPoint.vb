Option Explicit
Option Strict

' DBM
' Dynamic Bandwidth Monitor
' Leak detection method implemented in a real-time data historian
' J.H. Fitié, Vitens N.V.

Public Class DBMPoint

    Public DBMDataManager As DBMDataManager
    Public AbsoluteError(),RelativeError() As Double

    Public Sub New(ByVal DBMPointDriver As DBMPointDriver)
        DBMDataManager=New DBMDataManager(DBMPointDriver)
        ReDim AbsoluteError(DBMConstants.CorrelationPreviousPeriods)
        ReDim RelativeError(DBMConstants.CorrelationPreviousPeriods)
    End Sub

    Public Function Calculate(ByVal Timestamp As DateTime,ByVal IsInputDBMPoint As Boolean,ByVal HasCorrelationDBMPoint As Boolean,Optional ByRef SubstractDBMPoint As DBMPoint=Nothing) As DBMResult
        Dim CorrelationCounter,EMACounter,PatternCounter As Integer
        Dim Pattern(DBMConstants.ComparePatterns),CurrValueEMA(DBMConstants.EMAPreviousPeriods),PredValueEMA(DBMConstants.EMAPreviousPeriods),LowContrLimitEMA(DBMConstants.EMAPreviousPeriods),UppContrLimitEMA(DBMConstants.EMAPreviousPeriods) As Double
        Dim DBMStatistics As New DBMStatistics
        Calculate.Factor=0 ' No event
        For CorrelationCounter=0 To DBMConstants.CorrelationPreviousPeriods
            If CorrelationCounter=0 Or (IsInputDBMPoint And Calculate.Factor<>0 And HasCorrelationDBMPoint) Or Not IsInputDBMPoint Then
                For EMACounter=DBMConstants.EMAPreviousPeriods To 0 Step -1
                    If CorrelationCounter=0 Or (CorrelationCounter>0 And EMACounter=DBMConstants.EMAPreviousPeriods) Then
                        If CorrelationCounter>0 And EMACounter=DBMConstants.EMAPreviousPeriods Then ' Reuse calculation results when moving back for correlation calculation
                            CurrValueEMA=DBMFunctions.ArrayRotateLeft(CurrValueEMA)
                            PredValueEMA=DBMFunctions.ArrayRotateLeft(PredValueEMA)
                            LowContrLimitEMA=DBMFunctions.ArrayRotateLeft(LowContrLimitEMA)
                            UppContrLimitEMA=DBMFunctions.ArrayRotateLeft(UppContrLimitEMA)
                        End If
                        For PatternCounter=DBMConstants.ComparePatterns To 0 Step -1
                            Pattern(DBMConstants.ComparePatterns-PatternCounter)=DBMDataManager.Value(DateAdd("d",-PatternCounter*7,DateAdd("s",-(EMACounter+CorrelationCounter)*DBMConstants.CalculationInterval,Timestamp)))
                            If Not IsNothing(SubstractDBMPoint) Then
                                Pattern(DBMConstants.ComparePatterns-PatternCounter)-=SubstractDBMPoint.DBMDataManager.Value(DateAdd("d",-PatternCounter*7,DateAdd("s",-(EMACounter+CorrelationCounter)*DBMConstants.CalculationInterval,Timestamp)))
                            End If
                        Next PatternCounter
                        DBMStatistics.Calculate(DBMMath.RemoveOutliers(Pattern.Take(Pattern.Length-1).ToArray)) ' Calculate statistics for data after removing outliers
                        CurrValueEMA(EMACounter)=Pattern(DBMConstants.ComparePatterns)
                        PredValueEMA(EMACounter)=DBMConstants.ComparePatterns*DBMStatistics.Slope+DBMStatistics.Intercept ' Extrapolate linear regression
                        LowContrLimitEMA(EMACounter)=PredValueEMA(EMACounter)-DBMMath.ControlLimitRejectionCriterion(DBMStatistics.Count-1)*DBMStatistics.StDevSLinReg
                        UppContrLimitEMA(EMACounter)=PredValueEMA(EMACounter)+DBMMath.ControlLimitRejectionCriterion(DBMStatistics.Count-1)*DBMStatistics.StDevSLinReg
                    End If
                Next EMACounter
                AbsoluteError(DBMConstants.CorrelationPreviousPeriods-CorrelationCounter)=DBMMath.CalculateExpMovingAvg(PredValueEMA)-DBMMath.CalculateExpMovingAvg(CurrValueEMA) ' Absolute error compared to prediction
                RelativeError(DBMConstants.CorrelationPreviousPeriods-CorrelationCounter)=DBMMath.CalculateExpMovingAvg(PredValueEMA)/DBMMath.CalculateExpMovingAvg(CurrValueEMA)-1 ' Relative error compared to prediction
                If CorrelationCounter=0 Then
                    Calculate.CurrValue=DBMMath.CalculateExpMovingAvg(CurrValueEMA)
                    Calculate.PredValue=DBMMath.CalculateExpMovingAvg(PredValueEMA)
                    Calculate.LowContrLimit=DBMMath.CalculateExpMovingAvg(LowContrLimitEMA)
                    Calculate.UppContrLimit=DBMMath.CalculateExpMovingAvg(UppContrLimitEMA)
                    If Calculate.CurrValue<Calculate.LowContrLimit Then ' Lower control limit exceeded
                        Calculate.Factor=(Calculate.PredValue-Calculate.CurrValue)/(Calculate.LowContrLimit-Calculate.PredValue)
                    End If
                    If Calculate.CurrValue>Calculate.UppContrLimit Then ' Upper control limit exceeded
                        Calculate.Factor=(Calculate.CurrValue-Calculate.PredValue)/(Calculate.UppContrLimit-Calculate.PredValue)
                    End If
                End If
            End If
        Next CorrelationCounter
        Return Calculate
    End Function

End Class
