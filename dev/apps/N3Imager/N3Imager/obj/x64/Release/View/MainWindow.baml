   M S B A M L   `   `   `  ���� c  _System.Windows.Interactivity, Version=4.5.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35qPclr-namespace:System.Windows.Interactivity;assembly=System.Windows.InteractivitySystem.Windows.Interactivity  C ?N3Imager, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null-clr-namespace:N3Imager.ViewN3Imager.View 7 clr-namespace:N3Imager.ViewModelN3Imager.ViewModel a ]GongSolutions.WPF.DragDrop, Version=2.0.0.0, Culture=neutral, PublicKeyToken=91f1945125b7a587kLclr-namespace:GongSolutions.Wpf.DragDrop;assembly=GongSolutions.Wpf.DragDropGongSolutions.Wpf.DragDrop    N3Imager.View.MainWindow   R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     @d2http://schemas.microsoft.com/expression/blend/2008     Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     ^iPclr-namespace:System.Windows.Interactivity;assembly=System.Windows.Interactivity     h dMicrosoft.Expression.Interactions, Version=4.5.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35Bei9http://schemas.microsoft.com/expression/2010/interactions  'localclr-namespace:N3Imager.View  )vm clr-namespace:N3Imager.ViewModel  UddLclr-namespace:GongSolutions.Wpf.DragDrop;assembly=GongSolutions.Wpf.DragDrop    � Title$  	N3 Imager��$	��450��$	��650��$	��450��$	��650�� � 	Resources    	BoolToVis,  &N3Imager.ViewModel.ByteToBoolConverter  NumConverter0  *N3Imager.ViewModel.InverseBooleanConverter  InvBoolConverter��%-   &	        &	      &	 
     ��      J� InputBindings �� 
 �� Key$ F1�� �� Command ��  �� Path$  DataContext.CommandAddMask� �� ElementName$ Analyzer��.���  � RowDefinitions �� $
��Auto ��� $��* ��� $
��Auto �.+��� $��0��.+�`�  � DataContext �� *MeasurementVM+.�c� -   	 `� IsLocked	 . .@��� 
 � ToolTip$
 Start Dark Background��  ToolbarButton#��[ ���� *CommandStartDarkBG+.���� $4��./N3Imager;component/Images/dark_background.png��$��32���� $
 	Start Led��#��[ ���� *CommandStartLed+.���� $��..\Images\led.png��$��32���� $
 N3 Fluorescence������ *CommandStartN3Fluorescence+.���� $ ��..\Images\fluorescence.png��$��32���� $
 Deep UV Fluorescence��$��0������ * CommandStartDeepUVFluorescence+.���� $,��&/N3Imager;component/Images/deep_uv.png��$��32���� $
 Deep UV Fluorescence 2��$��1������ * CommandStartDeepUVFluorescence+.���� $)��#/N3Imager;component/Images/UV_2.png��$��32���� $
 Start Phosphorescence��#��[ ���� *CommandStartPhosphorescence+.���� $#��..\Images\phosphorescence.png��$��32���� $
 Turn off light source��#��[ ���� *
CommandEnd+.���� $��..\Images\off.png��$��32���� �� $
 Open save settings��#��[ ���� *CommandSaveSettings+.���� $!��..\Images\save_settings.png���� $��1�� �� SelectedIndex �� *
TabIndex+ �� Mode$ TwoWay=�.@��� $��Camera��.���  �� *
CameraVM+{��� *Ready+ �� $
��Auto ��� .+���  �� Orientation$ 
Horizontal=�.+�`� .�c� -   	 . .@��� $
 
Save image��#��[ ���� *CommandSave+.���� $��..\Images\save.png��� $��1�� � ColumnDefinitions �� $��3* ��� .+�� $��0�� �� $
��Auto ��� .+�  N3Imager.View.ZoomBorder  -     Name$ borderImage��  N3Imager.View.ZoomBorder"   PreviewMouseRightButtonDown$- 'borderImage_PreviewMouseRightButtonDown��$��1��$��5q�}�. 
���   �$��1q�   Child. �� $��Center=����� *CameraImage+�� $��0��
�������$��10(�$��Center=�.��ZXUse Mouse wheel to Zoom image. Left click and drag to pan. Right click to restore image.�� $
��Auto=�$
��Auto=�$��1��$��5q�.���  �� ��  �� $
��Auto ��� .+��� $��2��.+��� .��Camera Settings�� $��3,5,0,15q�$
��Left=�$	��100������ *CommandResetSettings+.��Reset�� $��1��.+��� -    ��Name$ lblImageWidth��.��Image Width:�� .��Image Height:�� .��Shutter [ms]:�� .��Gain:�� $y�	Collapsed=�.��FrameRate [ms]:�� $y�	Collapsed=�.��
W.B.(Red):�� $y�	Collapsed=�.��W.B.(Blue):�� $��1��$��1��.+��� {�.  ���� *
ImageWidth+���� $ lblImageWidth��$ ActualHeight��� {�.  ���� *ImageHeight+���� $ lblImageWidth��$ ActualHeight��� {�. ���� *	Shutter+$ TwoWay=� �� UpdateSourceTrigger$ PropertyChanged=����� $ lblImageWidth��$ ActualHeight��� {�. ���� *Gain+$ TwoWay=�$ PropertyChanged=����� $ lblImageWidth��$ ActualHeight��� $y�	Collapsed=�{�. ���� *	FrameRate+$ TwoWay=�$ PropertyChanged=����� $ lblImageWidth��$ ActualHeight��� $y�	Collapsed=�{�. ���� *WBred+$ TwoWay=�$ PropertyChanged=����� $ lblImageWidth��$ ActualHeight��� $y�	Collapsed=�{�. ���� *WBblue+$ TwoWay=�$ PropertyChanged=����� $ lblImageWidth��$ ActualHeight��� $��Result�� �� *
ResultVM+.���  �� $
��0.2* ��� $��0.55* ��� $��0.25* �.+��� 
�������._��� $��5q�.+��� ���� *CommandSaveAll+.��Save Image[s]�� $��1��
�������)  #GongSolutions.Wpf.DragDrop.DragDrop   IsDropTarget .    DropHandler �� ._��  �� $
��Auto ��� $��* ��� $
��Auto ��� $
��Auto �.+��� $��Center=����� *ImageFileName+
 �� *ImageFilePath+� $��1��
������ '  !N3Imager.ViewModel.MouseBehaviour   MouseUpCommand �� *MouseUpCommand+   MouseDownCommand �� *MouseDownCommand+   MouseMoveCommand �� *MouseMoveCommand+.+��� $��Center=����� *	PhosImage+�� .+��� 
��� ������	#77FFFFFFy��� *
IsDragging+ �� 	Converter# [   �� FallbackValue$ Hidden������ *
RoiWidth+���� *	RoiHeight+z�T�  T� X �� *RoiX+ T� Y �� *RoiY+�� $��2��$ 
Horizontal=�$��Center=�.+��� $��50������ *PreviousPhosImageCmd+.��
Previous�� $��30������ *PhosImageListIndex+$ TwoWay=��� $��50������ *NextPhosImageCmd+.��Next�� $��2��
�������._��� $��5q�.+��� .���� $��	Underline{�Image]� .+��� .��
Average L:�� $��Center=�{�.  $��0q����� *ImageL+$ OneWay=�]� .+��� .��
Average S:�� $��Center=�{�.  $��0q����� *ImageS+$ OneWay=�]� .+��� .��
Average H:�� $��Center=�{�.  $��0q����� *ImageH+$ OneWay=�]� .+��� �� .���� $��	Underline{�ROI]� .+��� .��
Average L:�� $��Center=�{�.  $��0q����� *
SelectionL+$ OneWay=�]� .+��� .��
Average S:�� $��Center=�{�.  $��0q����� *
SelectionS+$ OneWay=�]� .+��� .��
Average H:�� $��Center=�{�.  $��0q����� *
SelectionH+$ OneWay=��� $��Analyzer��.��� -   $��Analyzer�� �� *
AnalyzerVM+ �� $��4* ��� $��2* �.+�� $��5q� �� $��* ��� $
��Auto �.+�� $��0��
�������.+��� -    ��Name$ iImage��$��Center=����� *	CurrImage+(�  (� Stretch$
 Fill=����� $ iImage��$ ActualWidth����� $ iImage��$ ActualHeight�.���� -    ��Name$ CanvasResult��
������   J� IsHitTestVisible  . ���� $ Width����� $ Height�.   (System.Windows.Interactivity.Interaction!   Triggers! /   )System.Windows.Interactivity.EventTrigger  "   	EventName$" PreviewMouseDown��.	   (System.Windows.Interactivity.TriggerBase# 	  Actions.# >
  8Microsoft.Expression.Interactivity.Core.CallMethodAction
  $ 
  
MethodName$$ MouseDownHandler��% 
  TargetObject% ��   $" PreviewMouseMove��.# 
  $$ MouseMoveHandler��% ��   $" PreviewMouseUp��.# 
  $$ MouseUpHandler��% �� �� $��1��$ 
Horizontal=�$��Center=�$��5q�.+��� $��70��#��[ ���� *CommandPreviousItem+.��
Previous�� $��30��$��20,0,0,0q�$��Center=�$��center=����� *CurrentItem+$ TwoWay=�.���� .��/�� $��30��$��0,0,20,0q����� *
NumItems+�� $��70��#��[ ���� *CommandNextItem+.��Next� $��1��$��5q� �� $
��Auto ��� $
��Auto ��� $��* �.+��� $
��5, 0q�.+��� $
 Measurement Type��$	��0,5q����� *MeasurementTypes+& �� SelectedItem& �� *MeasurementType+$ TwoWay=����� .R��� ���� �� $ 
Horizontal=�$��Center=�$
��0, 5q�.+��� $��85��#��[ $��
0, 0, 5, 0q�$��Add Mask������ *CommandAddMask+�� $��85��$��
5, 0, 0, 0q�' �� TextAlignment$' Center=����� *
MaskCounts+�� $ 
Horizontal=�$��Center=�$
��0, 5q�.+��� $��85��#��[ $��
0, 0, 5, 0q����� *CommandRemoveLastOne+.��
Remove One�� $��85��#��[ $��
5, 0, 0, 0q����� *CommandRemoveMasks+.��
Remove All�� $ 
Horizontal=�$��Center=�$
��0, 5q�.+��� $��85��#��[ $��
0, 0, 5, 0q����� *CommandAnalyze+.��	Analyze�� $��85��#��[ $��
5, 0, 0, 0q����� *CommandSave+.��Save�� $��1��$��10, 5, 10, 10q�� $��2��$
��5, 0q�.+��� $��0q����� *
ResultList+( �� View( �� .P��� -   col1��$��ID��) �� Width$) 40��* �� DisplayMemberBinding* �� *Index+�� $��Description��) �� $ helperField��$ ActualWidth�* �� *Description+�� $��Comment��) �� $ helperField2��$ ActualWidth�+ �� CellTemplate+ �� .R��� ���� *	Comment+���� , �� 
TargetType", ��.��� - �� Property- � ��. �� Value
. �   �/ �� Triggers/ �� 0 �� Binding0 �� *	Comment+1 �� Value1 Natural.a��� - � ��
. ��� ��� 0 �� *	Comment+	1 Refer.a��� - � ��
. �� ���� 0 �� *	Comment+1 HPHT.a��� - � ��
. � � ��� 0 �� *	Comment+1 CVD.a��� - � ��
. � �|��� 0 �� *	Comment+1 CZ.a��� - � ��
. �r����� 0 �� *	Comment+1 Natural(DeepUV).a��� - � ��
. � ����� 0 �� *	Comment+1 Natural(shortPHOS).a��� - � ��
. �  ��� $y�Hidden=� �� ���� $
 col1��$ ActualWidth��� $��* ��� $��* �.+�� -	   2 �Name$2 helperField��$��1��� -
   $2 helperField2��$��2���� $��2�� �� *
StatusVM+.@��  �� $
��Auto ��� .+�� -   3 �Name$3 pgBar��$	��100��4 � IsIndeterminate4 �� *Busy+# [ y��� $ pgBar��$ IsIndeterminate�# [  �� $��1������ *CurrentMessage+