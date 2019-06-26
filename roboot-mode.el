(defconst roboot-mode-syntax-table
  (let ((table (make-syntax-table)))
    ;; ' is a string delimiter
    (modify-syntax-entry ?' "\"" table)
    ;; " is a string delimiter too
    (modify-syntax-entry ?\" "\"" table)
    (modify-syntax-entry ?\\ "\\" table)

    (modify-syntax-entry ?# "< b" table)
    (modify-syntax-entry ?\n "> b" table)

    (modify-syntax-entry ?\( "((")
    (modify-syntax-entry ?\) "))")

    (modify-syntax-entry ?\{ "({")
    (modify-syntax-entry ?\} ")}")

    (modify-syntax-entry ?\[ "([")
    (modify-syntax-entry ?\] ")]")

    table))

(setq roboot-highlights
      '(("\\b\\(module\\|if\\|else\\|fun\\|let\\|letmut\\|val\\|methodbase\\|while\\|struct\\|return\\|import\\|include\\|type\\|coercion\\|match\\)\\b" . font-lock-keyword-face)))

(define-derived-mode roboot-mode prog-mode "Roboot mode"
  :syntax-table roboot-mode-syntax-table
  (setq font-lock-defaults '(roboot-highlights))

  (set (make-local-variable 'indent-line-function) 'js-indent-line)
  (set (make-local-variable 'comment-start) "# ")
  (set (make-local-variable 'comment-start-skip) "#+\\s-*")

  (font-lock-fontify-buffer))
