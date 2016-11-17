Option Explicit
Option Strict

Public Class DBMConstants

    Public Const CalculationInterval As Integer =           300 ' seconds; 300 = 5 minutes
    Public Const ComparePatterns As Integer =               12 ' weeks
    Public Const EMAPreviousPeriods As Integer =            6 ' previous periods; 6 = 35 minutes, current value inclusive
    Public Const CorrelationPreviousPeriods As Integer =    CInt(2*3600/CalculationInterval-1) ' previous periods; 23 = 2 hours, current value inclusive
    Public Const CorrelationThreshold As Double =           0.83666 ' absolute correlation lower limit for detecting (anti)correlation

End Class
