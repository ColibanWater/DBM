﻿Option Explicit
Option Strict

' DBM
' Dynamic Bandwidth Monitor
' Leak detection method implemented in a real-time data historian
'
' Copyright (C) 2014, 2015, 2016 J.H. Fitié, Vitens N.V.
'
' This file is part of DBM.
'
' DBM is free software: you can redistribute it and/or modify
' it under the terms of the GNU General Public License as published by
' the Free Software Foundation, either version 3 of the License, or
' (at your option) any later version.
'
' DBM is distributed in the hope that it will be useful,
' but WITHOUT ANY WARRANTY; without even the implied warranty of
' MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
' GNU General Public License for more details.
'
' You should have received a copy of the GNU General Public License
' along with DBM.  If not, see <http://www.gnu.org/licenses/>.

Namespace DBMRt

    Public Class DBMRtPIPoint

        Private InputPointDriver,OutputPointDriver As DBM.DBMPointDriver
        Private CorrelationPoints As New Collections.Generic.List(Of DBM.DBMCorrelationPoint)

        Public Sub New(InputPIPoint As PISDK.PIPoint,OutputPIPoint As PISDK.PIPoint)
            Dim ExDesc,SubstringsA(),SubstringsB() As String
            InputPointDriver=New DBM.DBMPointDriver(InputPIPoint)
            OutputPointDriver=New DBM.DBMPointDriver(OutputPIPoint)
            ExDesc=DirectCast(OutputPointDriver.Point,PISDK.PIPoint).PointAttributes("ExDesc").Value.ToString
            If Text.RegularExpressions.Regex.IsMatch(ExDesc,"^[-]{0,1}[a-zA-Z0-9][a-zA-Z0-9_\.-]{0,}:[^:?*&]{1,}(&[-]{0,1}[a-zA-Z0-9][a-zA-Z0-9_\.-]{0,}:[^:?*&]{1,}){0,}$") Then
                SubstringsA=ExDesc.Split(New Char(){"&"c})
                For Each thisField In SubstringsA
                    SubstringsB=thisField.Split(New Char(){":"c})
                    Try
                        If DBMRtCalculator.PISDK.Servers(Mid(SubstringsB(0),1+If(Left(SubstringsB(0),1)="-",1,0))).PIPoints(SubstringsB(1)).Name<>"" Then
                            CorrelationPoints.Add(New DBM.DBMCorrelationPoint(New DBM.DBMPointDriver(DBMRtCalculator.PISDK.Servers(Mid(SubstringsB(0),1+If(Left(SubstringsB(0),1)="-",1,0))).PIPoints(SubstringsB(1))),Left(SubstringsB(0),1)="-"))
                        End If
                    Catch
                    End Try
                Next
            End If
        End Sub

        Public Sub Calculate
            Dim InputTimestamp,OutputTimestamp As PITimeServer.PITime
            InputTimestamp=DirectCast(InputPointDriver.Point,PISDK.PIPoint).Data.Snapshot.TimeStamp ' Timestamp of input point
            For Each thisCorrelationPoint In CorrelationPoints ' Check timestamp of correlation points
                InputTimestamp.UTCSeconds=Math.Min(InputTimestamp.UTCSeconds,DirectCast(thisCorrelationPoint.PointDriver.Point,PISDK.PIPoint).Data.Snapshot.TimeStamp.UTCSeconds) ' Timestamp of correlation point, keep earliest
            Next
            InputTimestamp.UTCSeconds-=DBM.DBMParameters.CalculationInterval+InputTimestamp.UTCSeconds Mod DBM.DBMParameters.CalculationInterval ' Can calculate output until (inclusive)
            OutputTimestamp=DirectCast(OutputPointDriver.Point,PISDK.PIPoint).Data.Snapshot.TimeStamp ' Timestamp of output point
            OutputTimestamp.UTCSeconds+=DBM.DBMParameters.CalculationInterval-OutputTimestamp.UTCSeconds Mod DBM.DBMParameters.CalculationInterval ' Next calculation timestamp
            If InputTimestamp.UTCSeconds>=OutputTimestamp.UTCSeconds Then ' If calculation timestamp can be calculated
                DirectCast(OutputPointDriver.Point,PISDK.PIPoint).Data.UpdateValue(DBMRtCalculator.DBM.Result(InputPointDriver,CorrelationPoints,InputTimestamp.LocalDate).Factor,InputTimestamp.LocalDate) ' Write calculated factor to output point
            End If
        End Sub

    End Class

End Namespace
