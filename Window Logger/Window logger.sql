CREATE TABLE ActiveWindowProcessLog (KeyId INTEGER PRIMARY KEY AUTOINCREMENT  NOT NULL, Date TEXT (10) NOT NULL, Time TEXT (8) NOT NULL, ProcessPath STRING NOT NULL, WindowTitle VARCHAR (200));

CREATE INDEX ActiveWindowProcessLog_Date ON ActiveWindowProcessLog (Date);