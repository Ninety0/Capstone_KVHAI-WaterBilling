DECLARE @m VARCHAR = 'lastname10';
DECLARE @v VARCHAR(100) = 'lastname10';
BEGIN
	SELECT @m AS [Value], LEN(@v) AS [Length];
	SELECT * FROM resident_tb WHERE (lname LIKE TRIM('%' + @v + '%') OR fname LIKE TRIM('%' + @v + '%'))  ORDER BY res_id OFFSET 0 ROWS FETCH NEXT 20 ROWS ONLY
END;

SELECT count(*) FROM resident_tb WHERE (lname like '%%' OR fname like '%%') AND activated = 'false'  ORDER BY res_id OFFSET 0 ROWS FETCH NEXT 20 ROWS ONLY
SELECT * FROM resident_tb WHERE concat(block,' ',lot) like '%400%' AND activated ='true' ORDER BY res_id OFFSET 0 ROWS FETCH NEXT 20 ROWS ONLY
SELECT concat('Blk ',block,' Lot ',lot) FROM resident_tb  
 use kvha1

SELECT concat(fname,' ',mname,' ',fname) FROM employee_tb where concat(fname,' ',mname,' ',fname) like '%oppa%'
ORDER BY emp_id OFFSET 0 ROWS FETCH NEXT 10 ROWS ONLY