::
:: %1: project dir
:: %2: debug release
:: %3: solution dir

::**********************************************************
:: copy bootstrap
::**********************************************************
:: copy %3packages\bootstrap.3.3.7\content\Content\*.css %1Content\*.css
:: copy %3packages\bootstrap.3.3.7\fonts\*.* %1fonts\*.*
:: copy %3packages\bootstrap.3.3.7\Scripts\*.js %1Scripts\*.js

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
:: copy Web.config
::**********************************************************
if exist %1Web-original.config copy %1Web-original.config %1Web.config
						
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