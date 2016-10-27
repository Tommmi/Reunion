::
:: %1: project dir
:: %2: debug release
:: %3: solution dir



::**********************************************************
:: fix error "error ASPCONFIG: It is an error to use a section registered as allowDefinition='MachineToApplication' beyond application level. This error can be caused by a virtual directory not being configured as an application in IIS.""
::**********************************************************
rd "%1obj" /S /Q
md "%1obj"
md "%1obj\Debug"
md "%1obj\Release"

::**********************************************************
:: copy jquery.ui
::**********************************************************
:: copy %1..\external\jquery-ui-1.12.0.custom\*.js %1Scripts\*.js
:: copy %1..\external\jquery-ui-1.12.0.custom\*.css %1Content\*.css
:: copy %1..\external\jquery-ui-1.12.0.custom\images\*.* %1Content\images\*.*
copy %1..\external\jquery-ui-1.12.0.custom\languages\datepicker-de.js %1Scripts\datepicker-de.js
copy %1..\external\jquery-ui-1.12.0.custom\languages\datepicker-es.js %1Scripts\datepicker-es.js

::**********************************************************
:: copy jquery.ui
::**********************************************************
copy %1..\external\jQuery.Validation.1.11.1\Content\Scripts\*.js %1Scripts\*.js

::**********************************************************
:: copy reunion.script.js
::**********************************************************
if exist %1Scripts\reunion.scripts.js attrib -r %1Scripts\reunion.scripts.js
echo copy %1..\Reunion.Scripts\bin\%2\Reunion.Scripts.js %1Scripts\reunion.scripts.js
copy %1..\Reunion.Scripts\bin\%2\Reunion.Scripts.js %1Scripts\reunion.scripts.js

::**********************************************************
:: copy angular
::**********************************************************
echo copy %3packages\angularjs.1.5.8\content\Scripts\angular-cookies.js %1Scripts\angular-cookies.js
copy %3packages\angularjs.1.5.8\content\Scripts\angular-cookies.js %1Scripts\angular-cookies.js
echo copy %3packages\angularjs.1.5.8\content\Scripts\angular-cookies.min.js %1Scripts\angular-cookies.min.js
copy %3packages\angularjs.1.5.8\content\Scripts\angular-cookies.min.js %1Scripts\angular-cookies.min.js


::**********************************************************
:: copy multiselectioncalendar
::**********************************************************
copy %1..\external\MultiSelectionCalendar\Scripts\*.js %1Scripts\*.js
copy %1..\external\MultiSelectionCalendar\Styles\*.* %1Content\*.*

						
::**********************************************************
:: generate stylesheet from LESS
::**********************************************************
set return=1
set lessfilename=Content\Reunion
goto lesscompile
:r1
						
						
:: *********************************
:: lesscompile
:: parameters: lessfilename
:: *********************************
goto end

:lesscompile
del %1%lessfilename%.css /f /q 
%3packages\dotless.1.5.2\tool\dotless.compiler.exe -m %1%lessfilename%.less %1%lessfilename%.css
goto r%return%

:: *********************************
:: end
:: *********************************
:end			