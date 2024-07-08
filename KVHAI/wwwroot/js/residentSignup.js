$(document).ready(function () {
    var currentTab = 0;
    var maxTab = 2;
    var minTab = 0;

    // Initialize
    updateTabDisplay();

    // Event listeners
    $('#cpass').on('change', ConfirmPassword);
    $('#btn-register').click(handleRegistration);
    $('#next-btn').click(IncrementTab);
    $('#prev-btn').click(DecrementTab);

    function IncrementTab() {
        if (currentTab < maxTab && validateCurrentTab()) {
            currentTab++;
            updateTabDisplay();
        }
        updateButtonVisibility();
    }

    function DecrementTab() {
        if (currentTab > minTab) {
            currentTab--;
            updateTabDisplay();
        }

        updateButtonVisibility();
    }

    function updateButtonVisibility() {
        $('.btn-prev').toggle(currentTab > minTab);
        $('.btn-next').toggle(currentTab < maxTab);
        $('#btn-register').toggle(currentTab == maxTab);

    }

    function updateTabDisplay() {
        $('.tab').hide().eq(currentTab).show();
        updateActiveClass();
    }

    function updateActiveClass() {
        $('.make-circle').removeClass('active').eq(currentTab).addClass('active');
    }

    function validateCurrentTab() {
        const currentTabElement = document.querySelectorAll('.tab')[currentTab];
        const inputs = currentTabElement.querySelectorAll('input, select');
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
        //console.log("jQuery version:", $.fn.jquery);
        //console.log("Password element:", $('#Password').length);
        //console.log("Password value:", $('#Password').val());
        //console.log("cpass element:", $('#cpass').length);
        //console.log("cpass value:", $('#cpass').val());

        const pass = document.getElementById('Password').value;
        const cpass = document.getElementById('cpass');
        const errorMsg = document.getElementById('err-msg');

        const isValid = pass === cpass.value;

        cpass.classList.toggle('is-invalid', !isValid);
        errorMsg.style.display = isValid ? 'none' : 'block';

        //if (!isValid) {
        //    alert("GGWP");
        //}

        return isValid;
    }

    function handleRegistration(e) {
        e.preventDefault();

        if (!validateCurrentTab()) {
            toastr.error('Please fill out all required fields correctly.');
            return;
        }

        var formData = new FormData($('#myForm')[0]);
        var fileInput = document.getElementById('Image');
        formData.append('file', fileInput.files[0]);

        $.ajax({
            type: 'POST',
            url: window.location.origin + '/kvhai/resident/signup',
            contentType: false,
            processData: false,
            data: formData,
            success: function (response) {
                console.log(response);
                const errorMessage = 'There was an error saving the resident and the image.';
                const successMessage = 'Registration Successful.';

                if (response.message.includes('error')) {
                    toastr.error(errorMessage);
                } else {
                    toastr.success(successMessage);
                }
            },
            error: function (xhr, status, error) {
                //console.log(xhr.responseText);
                toastr.error(xhr.responseText);
            }
        });
    }

    // Remove 'is-invalid' class on input
    $('input, select').on('input', function () {
        $(this).removeClass('is-invalid');
        $(this).closest('form').removeClass('was-validated');
    });

    // Toastr options
    toastr.options = {
        closeButton: true,
        progressBar: true,
        positionClass: 'toast-top-right',
        timeOut: 3000
    };
});


/*

function validateCurrentTab() {
        const currentTabElement = document.querySelectorAll('.tab')[currentTab];
        const form = document.querySelector('form');
        const inputs = currentTabElement.querySelectorAll('input, select');

        let isValid = true;

        inputs.forEach(input => {
            if (!input.checkValidity()) {
                isValid = false;
                input.classList.add('was-validated');
            } else {
                input.classList.remove('was-validated');
            }

            if (input.id === 'cpass' && !ConfirmPassword()) {
                isValid = false;
            }
        });

        form.classList.toggle('is-invalid', !isValid);
        form.classList.toggle('was-validated', !isValid);
        return isValid;
    }
*/