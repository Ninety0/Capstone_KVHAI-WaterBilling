$(document).ready(function () {

    $('.edit-btn').on('click', function () {
        var id = $(this).data('id');
        alert('Edit button clicked for Employee ID:' + id);
        var arr = {
            id: id
        }
        $.ajax({
            type: 'GET',
            url: '/adminaccount/GetEmployees',
            data: arr,
            success: function (response) {
                console.log(response);
                if (response.length > 0) {
                    var emp = response[0]; // Access the first element of the array

                    $('#employeeId').val(emp.emp_ID);
                    $('#Fname').val(emp.fname);
                    $('#Lname').val(emp.lname);
                    $('#Mname').val(emp.mname);
                    $('#Phone').val(emp.phone);
                    $('#Email').val(emp.email);
                    $('#Role').val(emp.role);
                    $('#employeeModal').modal('show');
                }
                else {
                    toastr.error('Failed to load employee data.');
                }
            },
            error: function (xhr, status, error) {
                toastr.error('Failed to load employee data.');
            }
        });
        // Add your edit logic here
    });

    // Toastr options
    toastr.options = {
        closeButton: true,
        progressBar: true,
        positionClass: 'toast-top-right',
        timeOut: 5000
    };

    // Event listeners
    $('#cpass').on('change', ConfirmPassword);
    $('#btn-register').click(handleRegistration);


    function validateCurrentTab() {
        const inputs = document.querySelectorAll('input, select');
        let isValid = true;

        inputs.forEach(input => {
            if (input.id === 'cpass') {
                // Handle confirm password separately
                if (!ConfirmPassword()) {
                    isValid = false;
                    input.classList.add('is-invalid');
                } else {
                    input.classList.remove('is-invalid');
                }
            } else {
                // Handle other inputs
                if (!input.checkValidity()) {
                    isValid = false;
                    input.classList.add('is-invalid');

                } else {
                    input.classList.remove('is-invalid');
                }
            }
        });

        

        return isValid;
    }
    


    function ConfirmPassword() {
        

        const pass = document.getElementById('Password').value;
        const cpass = document.getElementById('cpass');
        const errorMsg = document.getElementById('err-msg');

        const isValid = pass === cpass.value;

        cpass.classList.toggle('is-invalid', !isValid);
        errorMsg.style.display = isValid ? 'none' : 'block';

        return isValid;
    }

    function handleRegistration(e) {
        e.preventDefault();

        if (!validateCurrentTab()) {
            toastr.error('Please fill out all required fields correctly.');
            return;
        }

        //var formData = new FormData($('#form-emp')[0]);
        var formData = $('#form-emp').serialize();
        console.log(formData);

        $.ajax({
            type: 'POST',
            url: "/adminaccount/RegisterEmployee",
            contentType: 'application/x-www-form-urlencoded; charset=UTF-8', // when we use .serialize() this generates the data in query string format. this needs the default contentType (default content type is: contentType: 'application/x-www-form-urlencoded; charset=UTF-8') so it is optional, you can remove it
            data: formData,
            success: function (response) {
                if (response?.message?.includes('exist')) {
                    toastr.error('Email or Username already taken.');
                    return;
                }

                toastr.success('Registration Successful.');
                // Update your table here if needed
                $('#modal-employee').modal('hide');
                $(".modal-backdrop").remove();

                var result = $(response).find("#tableData").html();
                console.log("RESPONSE WHEN SUCCESS");
                console.log(result);
                $('#tableData').html(result);

                
            },
            error: function (xhr, status, error) {
                console.log(xhr.responseText);
                toastr.error('An error occurred while processing your request.');
            }
        });

        $('#modal-employee').on('hidden.bs.modal', function () {
            // Reset modal content when it's closed
            $('#form-emp')[0].reset();
        });
    }

    // Remove 'is-invalid' class on input
    $('input, select').on('input', function () {
        $(this).removeClass('is-invalid');
        $(this).closest('form').removeClass('was-validated');
    });

    function UpdateEmployee(e) {
        e.preventDefault();

        if (!validateCurrentTab()) {
            toastr.error('Please fill out all required fields correctly.');
            return;
        }

        //var formData = new FormData($('#form-emp')[0]);
        var formData = $('#form-emp').serialize();
        console.log(formData);

        $.ajax({
            type: 'POST',
            url: "/adminaccount/RegisterEmployee",
            contentType: 'application/x-www-form-urlencoded; charset=UTF-8', // when we use .serialize() this generates the data in query string format. this needs the default contentType (default content type is: contentType: 'application/x-www-form-urlencoded; charset=UTF-8') so it is optional, you can remove it
            data: formData,
            success: function (response) {
                if (response?.message?.includes('exist')) {
                    toastr.error('Email or Username already taken.');
                    return;
                }

                toastr.success('Registration Successful.');
                // Update your table here if needed
                $('#modal-employee').modal('hide');
                $(".modal-backdrop").remove();

                var result = $(response).find("#tableData").html();
                console.log("RESPONSE WHEN SUCCESS");
                console.log(result);
                $('#tableData').html(result);


            },
            error: function (xhr, status, error) {
                console.log(xhr.responseText);
                toastr.error('An error occurred while processing your request.');
            }
        });

        $('#modal-employee').on('hidden.bs.modal', function () {
            // Reset modal content when it's closed
            $('#form-emp')[0].reset();
        });
    }
});
