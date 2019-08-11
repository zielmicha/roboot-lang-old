(defun roboot-compiler-compile ()
  (interactive)
  (compile "~/mc/build.sh"))

(global-set-key (kbd "C-c C-l") 'roboot-compiler-compile)
