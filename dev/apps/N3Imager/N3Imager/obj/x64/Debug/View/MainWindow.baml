   M S B A M L   `   `   `  ����c  _System.Windows.Interactivity, Version=4.5.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35qPclr-namespace:System.Windows.Interactivity;assembly=System.Windows.InteractivitySystem.Windows.Interactivity  5   	   C ?N3Imager, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null-clr-namespace:N3Imager.ViewN3Imager.View 5   	   7 clr-namespace:N3Imager.ViewModelN3Imager.ViewModel 5	   	   a ]GongSolutions.WPF.DragDrop, Version=2.0.0.0, Culture=neutral, PublicKeyToken=91f1945125b7a587kLclr-namespace:GongSolutions.Wpf.DragDrop;assembly=GongSolutions.Wpf.DragDropGongSolutions.Wpf.DragDrop 5
   	      N3Imager.View.MainWindow   5   	   R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   ^iPclr-namespace:System.Windows.Interactivity;assembly=System.Windows.Interactivity     5   	   h dMicrosoft.Expression.Interactions, Version=4.5.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35Bei9http://schemas.microsoft.com/expression/2010/interactions  5   	   'localclr-namespace:N3Imager.View  5   	   )vm clr-namespace:N3Imager.ViewModel  5	   	   UddLclr-namespace:GongSolutions.Wpf.DragDrop;assembly=GongSolutions.Wpf.DragDrop  5
   	     � Title$  	N3 Imager��5   	   $	��450��6   $	��650��6(   $	��450��64   $	��650��6D    � 	Resources 5         	BoolToVis,  &N3Imager.ViewModel.ByteToBoolConverter  NumConverter0  *N3Imager.ViewModel.InverseBooleanConverter  InvBoolConverter��%W   &	        &	      &	 &     �� 5   
   6'     5   
   6!     5   
   6%   5       J� InputBindings 5      �� 5   
   
 �� Key$ F1��6    �� Command 6   ��  �� Path$  DataContext.CommandAddMask� �� ElementName$ Analyzer��.��5      �  � RowDefinitions 5   
   �� 5      $
��Auto �6   �� 5      $��* �6   �� 5      $
��Auto �6   .+�5   
   �� $��0��6   .+�5      `�  � DataContext 6   �� *MeasurementVM+.�5      c� -   5        	 `� IsLocked	 . .@�5      �� 
 � ToolTip$
 Start Dark Background��6     ToolbarButton#��[ 6=   ��5       �� *CommandStartDarkBG+.��5!      �� $4��./N3Imager;component/Images/dark_background.png��6    $��32��6X   5"      �� 5#      $
 	Start Led��6   #��[ 61   ��5$      �� *CommandStartLed+.��5%      �� $��..\Images\led.png��6    $��32��6;   5&      �� 5'      $
 N3 Fluorescence��6   ��67   �� *CommandStartN3Fluorescence+.��5(      �� $ ��..\Images\fluorescence.png��6    $��32��6D   5)      �� 5*      $
 Deep UV Fluorescence��6   $��0��6o   ��6<   �� * CommandStartDeepUVFluorescence+.��5+      �� $,��&/N3Imager;component/Images/deep_uv.png��6    $��32��6P   5,      �� 5-      $
 Deep UV Fluorescence 2��6   $��1��6q   ��6>   �� * CommandStartDeepUVFluorescence+.��5.      �� $)��#/N3Imager;component/Images/UV_2.png��6    $��32��6M   5/      �� 50      $
 Start Phosphorescence��6   #��[ 6=   ��51      �� *CommandStartPhosphorescence+.��52      �� $#��..\Images\phosphorescence.png��6    $��32��6G   53      �� 54      $
 Turn off light source��6   #��[ 6=   ��55      �� *
CommandEnd+.��56      �� $��..\Images\off.png��6    $��32��6;   57      �� 58      �� 59      $
 Open save settings��6   #��[ 6:   ��5:      �� *CommandSaveSettings+.��5;      �� $!��..\Images\save_settings.png��6    5<      5=      5>      5?      �� 5A   
   $��1��6    �� SelectedIndex 6"   �� *
TabIndex+ �� Mode$ TwoWay=�.@�5B      �� $��Camera��6   .��5C      �  6   �� *
CameraVM+{�68   �� *Ready+ 5D      �� 5E      $
��Auto �6(   68   �� 5F      6*   .+�5H      ��  �� Orientation$ 
Horizontal=�6!   .+�5I      `� .�5J      c� -   5        	 . .@�5K   "   �� $
 
Save image��6)   #��[ 5L   J   ��6)   �� *CommandSave+.��5M   &   �� $��..\Images\save.png��6,   5N   #   5O      5P      5Q      � 5R      $��1��6    � ColumnDefinitions 5S      �� 5T      $��3* �6/   6<   �� 5U      61   .+�5W      � $��0��6    5X      �� 5Y   "   $
��Auto �60   6@   �� 5Z   "   62   .+�5\        N3Imager.View.ZoomBorder  -   5          Name$ borderImage��5\   /     N3Imager.View.ZoomBorder"   PreviewMouseRightButtonDown$- 'borderImage_PreviewMouseRightButtonDown��5]   �   $��1��6/   $��5q�6<   }�. 
���   �$��1q�6o      Child. 5^   "   �� $��Center=�6G   ��6(   �� *CameraImage+5`   #   5a      �� 5b      $��0��6$   
�������$��10(�6D   $��Center=�5c   $   .��6A   ZXUse Mouse wheel to Zoom image. Left click and drag to pan. Right click to restore image.6�   5d      �� 5e      $
��Auto=�6'   $
��Auto=�6L   $��1��5f   '   $��5q�67   .��5g      �  5h   "   �� 5i   &   69   �� 5j   &   69    5l   "   �� 5m   &   $
��Auto �64   6D   �� 5n   &   66   .+�5p   "   �� $��2��6-   .+�5q   &   �� .��6,   Camera Settings6=   �� 5r   &   $��3,5,0,15q�6-   $
��Left=�6?   $	��100��6Z   ��5s   -   �� *CommandResetSettings+.��6V   Reset6]   5t   #   �� 5v   "   $��1��6-   .+�5w   &   �� -   5         ��Name$ lblImageWidth��5w   ,   .��6C   Image Width:6Q   �� 5x   &   .��6,   Image Height:6;   �� 5y   &   .��6,   Shutter [ms]:6;   �� 5z   &   .��6,   Gain:63   �� 5{   &   $y�	Collapsed=�6,   .��6C   FrameRate [ms]:6T   �� 5|   &   $y�	Collapsed=�6,   .��6C   
W.B.(Red):6O   �� 5}   &   $y�	Collapsed=�6,   .��6C   W.B.(Blue):6P   5~   #   �� 5   "   $��1��6-   $��1��6:   .+�5�   &   �� {�.  ��6.   �� *
ImageWidth+��5�   .   �� $ lblImageWidth��$ ActualHeight�6p   �� 5�   &   {�.  ��6.   �� *ImageHeight+��5�   .   �� $ lblImageWidth��$ ActualHeight�6p   �� 5�   &   {�. ��6.   �� *	Shutter+$ TwoWay=� �� UpdateSourceTrigger$ PropertyChanged=���5�   .   �� $ lblImageWidth��$ ActualHeight�6p   �� 5�   &   {�. ��6.   �� *Gain+$ TwoWay=�$ PropertyChanged=���5�   .   �� $ lblImageWidth��$ ActualHeight�6p   �� 5�   &   $y�	Collapsed=�6.   {�. ��6E   �� *	FrameRate+$ TwoWay=�$ PropertyChanged=���5�   .   �� $ lblImageWidth��$ ActualHeight�6p   �� 5�   &   $y�	Collapsed=�6.   {�. ��6E   �� *WBred+$ TwoWay=�$ PropertyChanged=���5�   .   �� $ lblImageWidth��$ ActualHeight�6p   �� 5�   &   $y�	Collapsed=�6.   {�. ��6E   �� *WBblue+$ TwoWay=�$ PropertyChanged=���5�   .   �� $ lblImageWidth��$ ActualHeight�6p   5�   #   5�      5�      5�      5�      5�      �� 5�      $��Result��6    6&   �� *
ResultVM+.��5�      �  5�      �� 5�      $
��0.2* �6+   �� 5�      $��0.55* �6+   �� 5�      $��0.25* �6+   .+�5�      �� 
�������._�5�      �� $��5q�6%   .+�5�      �� ��6&   �� *CommandSaveAll+.��6J   Save Image[s]5�      5�      5�      �� 5�      $��1��6   
�������)  #GongSolutions.Wpf.DragDrop.DragDrop   IsDropTarget .    DropHandler 5�      �� ._�5�      �  5�      �� 5�   "   $
��Auto �60   �� 5�   "   $��* �60   �� 5�   "   $
��Auto �60   �� 5�   "   $
��Auto �60   .+�5�      �� $��Center=�5�   %   ��5�   %   �� *ImageFileName+
 6G   �� *ImageFilePath+5�   D   � 5�      $��1��6#   
������ '  !N3Imager.ViewModel.MouseBehaviour   MouseUpCommand 5�   #   �� *MouseUpCommand+   MouseDownCommand 5�   #   �� *MouseDownCommand+   MouseMoveCommand 5�   #   �� *MouseMoveCommand+.+�5�   "   �� $��Center=�6E   ��6(   �� *	PhosImage+5�   #   �� 5�   "   .+�5�   &   �� 
��� ������	#77FFFFFFy�5�   )   �� *
IsDragging+ �� 	Converter# [   �� FallbackValue$ Hidden����5�   )   �� *
RoiWidth+��6D   �� *	RoiHeight+z�5�   *   T� 5�   .    T� X 6A   �� *RoiX+ T� Y 6T   �� *RoiY+5�   '   5�   #   5�      �� 5�      $��2��6)   $ 
Horizontal=�66   $��Center=�6O   .+�5�   "   �� $��50��6)   ��64   �� *PreviousPhosImageCmd+.��6]   
Previous6g   �� 5�   "   $��30��6*   ��65   �� *PhosImageListIndex+$ TwoWay=�6h   �� 5�   "   $��50��6)   ��64   �� *NextPhosImageCmd+.��6Y   Next6_   5�      5�      5�      �� 5�      $��2��6   
�������._�5�      �� $��5q�6%   .+�5�      �� .��5�   "   �� $��	Underline{�6,   Image6O   5�      ]� 5�      .+�5�   "   �� .��6(   
Average L:64   �� 5�   "   $��Center=�6*   {�.  $��0q�5�   <   ��5�   *   �� *ImageL+$ OneWay=�6R   5�      ]� 5�      .+�5�   "   �� .��6(   
Average S:64   �� 5�   "   $��Center=�6*   {�.  $��0q�5�   <   ��5�   *   �� *ImageS+$ OneWay=�6R   5�      ]� 5�      .+�5�   "   �� .��6(   
Average H:64   �� 5�   "   $��Center=�6*   {�.  $��0q�5�   <   ��5�   *   �� *ImageH+$ OneWay=�6R   5�      ]� 5�      .+�5�   "   �� 6*   5�      �� 5�      .��5�   "   �� $��	Underline{�6,   ROI6M   5�      ]� 5�      .+�5�   "   �� .��6(   
Average L:64   �� 5�   "   $��Center=�6*   {�.  $��0q�5�   <   ��5�   *   �� *
SelectionL+$ OneWay=�6V   5�      ]� 5�      .+�5�   "   �� .��6(   
Average S:64   �� 5�   "   $��Center=�6*   {�.  $��0q�5�   <   ��5�   *   �� *
SelectionS+$ OneWay=�6V   5�      ]� 5�      .+�5�   "   �� .��6(   
Average H:64   �� 5�   "   $��Center=�6*   {�.  $��0q�5�   <   ��5�   *   �� *
SelectionH+$ OneWay=�6V   5�      5�      5�      5      5     �� 5     $��Analyzer��6   .��5     � -   5        $��Analyzer��5  :    6   �� *
AnalyzerVM+ 5     �� 5     $��4* �6+   �� 5     $��2* �6+   .+�5     � $��5q�6    5	     �� 5
     $��* �6,   �� 5     $
��Auto �6,   .+�5     � $��0��6   
�������.+�5     �� -   5         ��Name$ iImage��5  $   $��Center=�6Q   ��64   �� *	CurrImage+6n   (� 5      (� Stretch$
 Fill=�6&   ��65   �� $ iImage��$ ActualWidth���6l   �� $ iImage��$ ActualHeight�.��5  "   �� -   5         ��Name$ CanvasResult��5  C   
������   J� IsHitTestVisible  . ��6q   �� $ Width���6�   �� $ Height�.   (System.Windows.Interactivity.Interaction!   Triggers! 5  &   /   )System.Windows.Interactivity.EventTrigger  5  *   "   	EventName$" PreviewMouseDown��69   .	   (System.Windows.Interactivity.TriggerBase# 	  Actions.# 5  *   >
  8Microsoft.Expression.Interactivity.Core.CallMethodAction
  $ 
  
MethodName$$ MouseDownHandler��6>   % 
  TargetObject% 6\   �� 5  +     5  *   $" PreviewMouseMove��69   .# 5  *   
  $$ MouseMoveHandler��6>   % 6\   �� 5  +     5  *   $" PreviewMouseUp��69   .# 5  *   
  $$ MouseUpHandler��6>   % 6Z   �� 5  +   5  #   5     5      �� 5!     $��1��6%   $ 
Horizontal=�62   $��Center=�6K   $��5q�6h   .+�5"     �� $��70��6%   #��[ 60   ��6W   �� *CommandPreviousItem+.��6   
Previous6�   �� 5#     $��30��6&   $��20,0,0,0q�61   $��Center=�6C   $��center=�6`   ��5$  &   �� *CurrentItem+$ TwoWay=�.��6P   5%     �� 5&     .��6$   /6'   �� 5'     $��30��6$   $��0,0,20,0q�6/   ��6A   �� *
NumItems+6`   �� 5(     $��70��6%   #��[ 60   ��6W   �� *CommandNextItem+.��6{   Next6�   5)     5*     � 5+     $��1��6   $��5q�6+    5,     �� 5-     $
��Auto �6,   6<   �� 5.     $
��Auto �6,   6<   �� 5/     $��* �6,   .+�51     �� $
��5, 0q�6%   .+�52     �� $
 Measurement Type��6'   $	��0,5q�6B   ��6O   �� *MeasurementTypes+& �� SelectedItem& 6x   �� *MeasurementType+$ TwoWay=���53  "   �� 54  &   .R�55  *   �� ��64   �� 56  '   58     �� 59     $ 
Horizontal=�6)   $��Center=�6B   $
��0, 5q�6_   .+�5:  "   �� $��85��6)   #��[ 64   $��
0, 0, 5, 0q�6~   $��Add Mask��6�   ��6[   �� *CommandAddMask+6�   �� 5;  "   $��85��6,   $��
5, 0, 0, 0q�67   ' �� TextAlignment$' Center=�6g   ��6K   �� *
MaskCounts+6g   5<     �� 5=     $ 
Horizontal=�6)   $��Center=�6B   $
��0, 5q�6_   .+�5>  "   �� $��85��6)   #��[ 64   $��
0, 0, 5, 0q�6�   ��6[   �� *CommandRemoveLastOne+.��6�   
Remove One6�   �� 5?  "   $��85��6)   #��[ 64   $��
5, 0, 0, 0q�6�   ��6[   �� *CommandRemoveMasks+.��6�   
Remove All6�   5@     �� 5A     $ 
Horizontal=�6)   $��Center=�6B   $
��0, 5q�6_   .+�5B  "   �� $��85��6)   #��[ 64   $��
0, 0, 5, 0q�6~   ��6[   �� *CommandAnalyze+.��6�   	Analyze6�   �� 5C  "   $��85��6)   #��[ 64   $��
5, 0, 0, 0q�6{   ��6[   �� *CommandSave+.��6�   Save6�   5D     5E     �� 5F     $��1��6$   $��10, 5, 10, 10q�61   � 5G     $��2��6   $
��5, 0q�6,   .+�5H     �� $��0q�6'   ��62   �� *
ResultList+( �� View( 5I  "   �� 5J  &   .P�5K  *   �� -   5        col1��$��ID��5K  G   ) �� Width$) 40��6S   * �� DisplayMemberBinding* 6^   �� *Index+�� 5L  *   $��Description��69   ) 6N   �� $ helperField��$ ActualWidth�* 6�   �� *Description+�� 5M  *   $��Comment��69   ) 6J   �� $ helperField2��$ ActualWidth�+ �� CellTemplate+ 5N  .   �� 5O  2   .R�5P  6   �� ��6@   �� *	Comment+��5Q  :   �� 5R  >   , �� 
TargetType", ��6D   .�5S  B   �� - �� Property- � ��. �� Value
. �   �6_   / �� Triggers/ 5T  B   �� 5U  F   0 �� Binding0 6R   �� *	Comment+1 �� Value1 Natural6n   .a�5V  J   �� - � ��
. ��� �6g   5W  G   �� 5X  F   0 6R   �� *	Comment+	1 Refer6n   .a�5Y  J   �� - � ��
. �� ��6g   5Z  G   �� 5[  F   0 6R   �� *	Comment+1 HPHT6n   .a�5\  J   �� - � ��
. � � �6g   5]  G   �� 5^  F   0 6R   �� *	Comment+1 CVD6n   .a�5_  J   �� - � ��
. � �|�6g   5`  G   �� 5a  F   0 6R   �� *	Comment+1 CZ6n   .a�5b  J   �� - � ��
. �r���6g   5c  G   �� 5d  F   0 6R   �� *	Comment+1 Natural(DeepUV)6n   .a�5e  J   �� - � ��
. � ���6g   5f  G   �� 5g  F   0 6R   �� *	Comment+1 Natural(shortPHOS)6n   .a�5h  J   �� - � ��
. �  ��6g   5i  G   5k  ?   5m  7   5n  3   5p  +   5q  '   5�     � 5�     $y�Hidden=�6#    5�  "   �� 5�  &   ��67   �� $
 col1��$ ActualWidth��� 5�  &   $��* �67   �� 5�  &   $��* �67   .+�5�  "   � -	   5        2 �Name$2 helperField��5�  7   $��1��6'   67   � 5�  "   -
   5        $2 helperField2��5�  7   $��2��6'   67   5�     5�     5�     5�     5�     5�     �� 5�  
   $��2��6    6!   �� *
StatusVM+.@�5�     �  5�     �� 5�     $
��Auto �6'   66   �� 5�     6)   .+�5�     � -   5        3 �Name$3 pgBar��5�     $	��100��6w   4 � IsIndeterminate4 6-   �� *Busy+# [ y�5�     �� $ pgBar��$ IsIndeterminate�# [  �� 5�     $��1��5�     ��5�     �� *CurrentMessage+5�  +   5�     5�     5�     5�     