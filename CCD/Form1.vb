Imports SharpDX

Public Class Form1

    Public Rnd As New Random

    Public IKChainRoot As IK = Nothing

    Public Target As Vector3 = Vector3.Zero

    Public CanvasBitmap As Bitmap = Nothing

    Public CanvasGraphics As Graphics = Nothing

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        CanvasBitmap = New Bitmap(1000, 800)
        CanvasGraphics = Graphics.FromImage(CanvasBitmap)
        PBox.Image = CanvasBitmap
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        IKChainRoot = New IK With {.StartPosition = New Vector3(500, 700, 0), .EndPosition = New Vector3(500, 500, 0), .Length = 200}
        Dim ik1 As New IK With {.StartPosition = New Vector3(500, 500, 0), .EndPosition = New Vector3(500, 300, 0), .Length = 200}
        Dim ik2 As New IK With {.StartPosition = New Vector3(500, 300, 0), .EndPosition = New Vector3(500, 150, 0), .Length = 150}
        Dim ik3 As New IK With {.StartPosition = New Vector3(500, 150, 0), .EndPosition = New Vector3(500, 50, 0), .Length = 100}
        IKChainRoot.Children = ik1
        ik1.Parent = IKChainRoot
        ik1.Children = ik2
        ik2.Parent = ik1
        ik2.Children = ik3
        ik3.Parent = ik2

        Target = New Vector3(200 + 600 * rnd.NextDouble, 50 + 600 * rnd.NextDouble, 0)

        DrawCanvas()
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        CCD_Step()
        DrawCanvas()
    End Sub


    Public Sub DrawCanvas()
        Const CIRCLE_RADIUS As Single = 18.0F
        With CanvasGraphics
            .Clear(Drawing.Color.White)

            Dim iter_ik As IK = IKChainRoot
            While iter_ik IsNot Nothing
                .DrawEllipse(Pens.Black, New Drawing.Rectangle(
                             iter_ik.StartPosition.X - CIRCLE_RADIUS, iter_ik.StartPosition.Y - CIRCLE_RADIUS,
                             CIRCLE_RADIUS * 2, CIRCLE_RADIUS * 2))
                .DrawLine(Pens.Black, New Drawing.Point(iter_ik.StartPosition.X, iter_ik.StartPosition.Y),
                          New Drawing.Point(iter_ik.EndPosition.X, iter_ik.EndPosition.Y))
                iter_ik = iter_ik.Children
            End While

            .DrawEllipse(Pens.Red, New Drawing.Rectangle(
                             Target.X - CIRCLE_RADIUS, Target.Y - CIRCLE_RADIUS,
                             CIRCLE_RADIUS * 2, CIRCLE_RADIUS * 2))
        End With
        PBox.Invalidate()
    End Sub

    Public Sub CCD_Step()
        Dim tail As IK = IKChainRoot.GetTail()
        Dim current As IK = tail
        While current IsNot Nothing
            Dim se As Vector3 = current.GetChainSE()
            se.Normalize()
            Dim st As Vector3 = Target - current.StartPosition
            st.Normalize()
ADD_BIAS:
            If ((se - st).Length < 0.0001) Then
                If current.Children Is Nothing Then
                    GoTo NEXT_NODE
                Else
                    st = Target - current.StartPosition + New Vector3(Rnd.NextDouble * 0.1, Rnd.NextDouble * 0.1, 0)
                    st.Normalize()
                    GoTo ADD_BIAS
                End If
            End If
            Dim theta As Single = Math.Acos(Math.Clamp(Vector3.Dot(se, st), -1.0, 1.0))
            'If Single.IsNaN(theta) Then
            '    Throw New Exception
            'End If
            Dim axis As Vector3 = Vector3.Cross(se, st)
            axis.Normalize()
            current.Rotate(axis, theta)
NEXT_NODE:
            current = current.Parent
        End While
    End Sub

End Class


Public Class IK

    Public StartPosition As Vector3 = Vector3.Zero

    Public EndPosition As Vector3 = Vector3.Zero

    Public Length As Single = 0.0F

    Public Parent As IK = Nothing

    Public Children As IK = Nothing


    Public Function GetChainE() As Vector3
        If Me.Children Is Nothing Then
            Return Me.EndPosition
        End If
        Return Me.Children.GetChainE()
    End Function

    Public Function GetChainSE() As Vector3
        Dim s As Vector3 = Me.StartPosition
        Dim e As Vector3 = Me.GetChainE()
        Return (e - s)
    End Function

    Public Function GetTail() As IK
        If Me.Children Is Nothing Then
            Return Me
        End If
        Return Me.Children.GetTail()
    End Function

    Public Sub Rotate(axis As Vector3, theta As Single)
        Dim rot_mat As Matrix = Matrix.RotationAxis(axis, theta)
        Dim vec As Vector3 = Me.EndPosition - Me.StartPosition
        Dim vec_t As Vector3 = Vector3.TransformCoordinate(vec, rot_mat)
        Me.EndPosition = Me.StartPosition + vec_t
        If Me.Children IsNot Nothing Then
            Dim child_vec As Vector3 = Me.Children.EndPosition - Me.Children.StartPosition
            Me.Children.StartPosition = Me.EndPosition
            Me.Children.EndPosition = Me.Children.StartPosition + child_vec
            Me.Children.Rotate(axis, theta)
        End If
    End Sub

End Class
